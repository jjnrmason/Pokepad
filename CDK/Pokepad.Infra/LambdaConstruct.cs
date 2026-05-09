using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AwsApigatewayv2Authorizers;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using HttpMethod = Amazon.CDK.AWS.Apigatewayv2.HttpMethod;

namespace Pokepad.Infra;

public sealed class LambdaConstruct : Construct
{
    public LambdaConstruct(
        Construct scope,
        string id,
        DataLakeConstruct dataLake,
        GlueCatalogConstruct glueCatalog,
        CognitoConstruct cognito,
        DynamoConstruct dynamo)
        : base(scope, id)
    {
        var role = CreateLambdaRole(dataLake, glueCatalog, dynamo);
        var function = CreateLambdaFunction(role, dataLake, dynamo);
        CreateErrorRateAlarm(function);
        var api = CreateHttpApi(function, cognito);

        _ = new CfnOutput(this, "SearchApiUrl", new CfnOutputProps
        {
            Value = api.ApiEndpoint,
            Description = "Pokepad search API endpoint"
        });
    }

    private Role CreateLambdaRole(DataLakeConstruct dataLake, GlueCatalogConstruct glueCatalog, DynamoConstruct dynamo)
    {
        var stack = Stack.Of(this);

        var role = new Role(this, "lambda-role", new RoleProps
        {
            RoleName = "pokepad-search-lambda",
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            ManagedPolicies =
            [
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole")
            ]
        });

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:GetObject"],
            Resources = [dataLake.Gold.ArnForObjects("*")]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:ListBucket", "s3:GetBucketLocation"],
            Resources = [dataLake.Gold.BucketArn]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:PutObject", "s3:GetObject", "s3:AbortMultipartUpload", "s3:ListMultipartUploadParts"],
            Resources = [dataLake.AthenaResults.ArnForObjects("*")]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:ListBucket", "s3:GetBucketLocation"],
            Resources = [dataLake.AthenaResults.BucketArn]
        }));

        var catalogArn = Arn.Format(new ArnComponents { Service = "glue", Resource = "catalog" }, stack);
        var databaseArn = Arn.Format(new ArnComponents { Service = "glue", Resource = "database", ResourceName = glueCatalog.DatabaseName }, stack);
        var tableArn = Arn.Format(new ArnComponents { Service = "glue", Resource = "table", ResourceName = $"{glueCatalog.DatabaseName}/*" }, stack);

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["glue:GetDatabase", "glue:GetTable", "glue:GetTables", "glue:GetPartition", "glue:GetPartitions"],
            Resources = [catalogArn, databaseArn, tableArn]
        }));

        var workgroupArn = Arn.Format(new ArnComponents { Service = "athena", Resource = "workgroup", ResourceName = "pokepad" }, stack);

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions =
            [
                "athena:StartQueryExecution",
                "athena:GetQueryExecution",
                "athena:GetQueryResults",
                "athena:GetWorkGroup",
                "athena:StopQueryExecution"
            ],
            Resources = [workgroupArn]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["athena:ListWorkGroups"],
            Resources = ["*"]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["dynamodb:PutItem", "dynamodb:GetItem"],
            Resources = [dynamo.Table.TableArn]
        }));

        var ssmParamArn = Arn.Format(new ArnComponents
        {
            Service = "ssm",
            Resource = "parameter",
            ResourceName = "pokepad/anthropic-api-key"
        }, stack);

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["ssm:GetParameter"],
            Resources = [ssmParamArn]
        }));

        return role;
    }

    private Function CreateLambdaFunction(Role role, DataLakeConstruct dataLake, DynamoConstruct dynamo)
    {
        var athenaOutputLocation = $"s3://{dataLake.AthenaResults.BucketName}/results/";

        var logGroup = new LogGroup(this, "lambda-logs", new LogGroupProps
        {
            Retention = RetentionDays.ONE_MONTH,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        return new Function(this, "search-function", new FunctionProps
        {
            FunctionName = "pokepad-search",
            Runtime = Runtime.PROVIDED_AL2023,
            Handler = "Pokepad.Lambda",
            Code = Code.FromAsset(
                Path.Combine("..", "Lambda", "Pokepad.Lambda"),
                new Amazon.CDK.AWS.S3.Assets.AssetOptions
                {
                    Bundling = new BundlingOptions
                    {
                        Image = DockerImage.FromRegistry("mcr.microsoft.com/dotnet/sdk:10.0"),
                        Environment = new Dictionary<string, string>
                        {
                            { "HOME", "/tmp" },
                            { "DOTNET_CLI_HOME", "/tmp" },
                            { "DOTNET_NOLOGO", "1" },
                            { "DOTNET_CLI_TELEMETRY_OPTOUT", "1" },
                            { "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1" }
                        },
                        Command =
                        [
                            "bash", "-c",
                            "dotnet publish -c Release -r linux-x64 --self-contained true -o /tmp/publish && " +
                            "cp -r /tmp/publish/. /asset-output/ && " +
                            "mv /asset-output/Pokepad.Lambda /asset-output/bootstrap"
                        ]
                    }
                }),
            Role = role,
            MemorySize = 512,
            Timeout = Duration.Seconds(30),
            Architecture = Architecture.X86_64,
            LogGroup = logGroup,
            Tracing = Tracing.ACTIVE,
            Environment = new Dictionary<string, string>
            {
                { "ASPNETCORE_ENVIRONMENT", "Production" },
                { "ATHENA_OUTPUT_LOCATION", athenaOutputLocation },
                { "ANTHROPIC_API_KEY_PARAM", "/pokepad/anthropic-api-key" },
                { "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1" },
                { "DYNAMODB_TABLE_NAME", dynamo.Table.TableName }
            }
        });
    }

    private void CreateErrorRateAlarm(Function function)
    {
        var period = Duration.Minutes(5);

        var errorRate = new MathExpression(new MathExpressionProps
        {
            Expression = "IF(invocations > 0, 100 * errors / invocations, 0)",
            UsingMetrics = new Dictionary<string, IMetric>
            {
                ["errors"] = function.MetricErrors(new MetricOptions { Period = period }),
                ["invocations"] = function.MetricInvocations(new MetricOptions { Period = period })
            },
            Period = period,
            Label = "Error Rate %"
        });

        _ = new Alarm(this, "error-rate-alarm", new AlarmProps
        {
            AlarmName = "pokepad-lambda-error-rate",
            AlarmDescription = "Lambda error rate exceeded 5% over a 5-minute window",
            Metric = errorRate,
            Threshold = 5,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });
    }

    private HttpApi CreateHttpApi(Function function, CognitoConstruct cognito)
    {
        var stack = Stack.Of(this);
        var issuer = $"https://cognito-idp.{stack.Region}.amazonaws.com/{cognito.UserPool.UserPoolId}";
        var authorizer = new HttpJwtAuthorizer("cognito-authorizer", issuer, new HttpJwtAuthorizerProps
        {
            JwtAudience = [cognito.UserPoolClient.UserPoolClientId]
        });

        var accessLogGroup = new LogGroup(this, "api-access-logs", new LogGroupProps
        {
            Retention = RetentionDays.ONE_MONTH,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var integration = new HttpLambdaIntegration("search-integration", function);

        var api = new HttpApi(this, "search-api", new HttpApiProps
        {
            ApiName = "pokepad-search-api",
            Description = "Pokepad natural language search API",
            CorsPreflight = new CorsPreflightOptions
            {
                AllowOrigins = ["*"],
                AllowMethods = [CorsHttpMethod.POST, CorsHttpMethod.GET],
                AllowHeaders = ["Content-Type", "Authorization"],
                MaxAge = Duration.Days(1)
            }
        });

        var cfnStage = (CfnStage)api.DefaultStage!.Node.DefaultChild!;
        cfnStage.AccessLogSettings = new CfnStage.AccessLogSettingsProperty
        {
            DestinationArn = accessLogGroup.LogGroupArn,
            Format = "{\"requestId\":\"$context.requestId\",\"ip\":\"$context.identity.sourceIp\",\"requestTime\":\"$context.requestTime\",\"method\":\"$context.httpMethod\",\"routeKey\":\"$context.routeKey\",\"status\":\"$context.status\",\"responseLength\":\"$context.responseLength\",\"integrationLatency\":\"$context.integrationLatency\",\"errorMessage\":\"$context.error.message\"}"
        };
        cfnStage.DefaultRouteSettings = new CfnStage.RouteSettingsProperty
        {
            ThrottlingBurstLimit = 10,
            ThrottlingRateLimit = 10
        };

        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/v1/search",
            Methods = [HttpMethod.POST],
            Integration = integration,
            Authorizer = authorizer
        });

        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/v1/health",
            Methods = [HttpMethod.GET],
            Integration = integration
        });

        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/v1/query/start",
            Methods = [HttpMethod.POST],
            Integration = integration,
            Authorizer = authorizer
        });

        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/v1/query/{id}/status",
            Methods = [HttpMethod.GET],
            Integration = integration,
            Authorizer = authorizer
        });

        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/v1/query/{id}/results",
            Methods = [HttpMethod.GET],
            Integration = integration,
            Authorizer = authorizer
        });

        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/openapi/v1.json",
            Methods = [HttpMethod.GET],
            Integration = integration
        });

        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/scalar/{proxy+}",
            Methods = [HttpMethod.GET],
            Integration = integration
        });

        return api;
    }
}
