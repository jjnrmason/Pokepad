using Amazon.CDK;
using Amazon.CDK.AWS.ApplicationAutoScaling;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace Pokepad.Infra.Constructs;

public sealed class EcsConstruct : Construct
{
    public EcsConstruct(
        Construct scope,
        string id,
        DataLakeConstruct dataLake,
        VectorStoreConstruct vectorStore)
        : base(scope, id)
    {
        var queue = CreateQueue();
        var cluster = CreateCluster(vectorStore.Vpc);
        var taskDef = CreateTaskDefinition(dataLake, vectorStore, queue);
        var service = CreateService(cluster, taskDef, vectorStore);
        ScaleOnQueueDepth(service, queue);
        CreateEventBridgeRule(dataLake, queue);
    }

    private Queue CreateQueue()
    {
        var dlq = new Queue(this, "indexer-dlq", new QueueProps
        {
            QueueName = "pokepad-embedding-indexer-dlq",
            RetentionPeriod = Duration.Days(14),
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        return new Queue(this, "indexer-queue", new QueueProps
        {
            QueueName = "pokepad-embedding-indexer-queue",
            // Must be >= the processing time of one file; workers use this as VisibilityTimeout
            VisibilityTimeout = Duration.Seconds(300),
            DeadLetterQueue = new DeadLetterQueue
            {
                Queue = dlq,
                MaxReceiveCount = 3
            },
            RemovalPolicy = RemovalPolicy.DESTROY
        });
    }

    private Cluster CreateCluster(Vpc vpc)
    {
        return new Cluster(this, "cluster", new ClusterProps
        {
            ClusterName = "pokepad",
            Vpc = vpc
        });
    }

    private FargateTaskDefinition CreateTaskDefinition(
        DataLakeConstruct dataLake,
        VectorStoreConstruct vectorStore,
        Queue queue)
    {
        var stack = Stack.Of(this);

        var executionRole = new Role(this, "execution-role", new RoleProps
        {
            AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
            ManagedPolicies =
            [
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy")
            ]
        });

        var taskRole = new Role(this, "task-role", new RoleProps
        {
            AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com")
        });

        taskRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:GetObject"],
            Resources = [dataLake.Gold.ArnForObjects("*")]
        }));

        taskRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:ListBucket", "s3:GetBucketLocation"],
            Resources = [dataLake.Gold.BucketArn]
        }));

        var aiKeyParamArn = Arn.Format(new ArnComponents
        {
            Service = "ssm",
            Resource = "parameter",
            ResourceName = "pokepad/ai-api-key"
        }, stack);

        var connStringParamArn = Arn.Format(new ArnComponents
        {
            Service = "ssm",
            Resource = "parameter",
            ResourceName = "pokepad/vector-db-connection-string"
        }, stack);

        taskRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["ssm:GetParameter"],
            Resources = [aiKeyParamArn, connStringParamArn]
        }));

        taskRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["secretsmanager:GetSecretValue"],
            Resources = [vectorStore.DbSecret.SecretArn]
        }));

        taskRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["sqs:ReceiveMessage", "sqs:DeleteMessage", "sqs:GetQueueAttributes"],
            Resources = [queue.QueueArn]
        }));

        var logGroup = new Amazon.CDK.AWS.Logs.LogGroup(this, "indexer-logs", new Amazon.CDK.AWS.Logs.LogGroupProps
        {
            Retention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_MONTH,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var image = new DockerImageAsset(this, "indexer-image", new DockerImageAssetProps
        {
            Directory = "..",
            File = "Services/Pokepad.EmbeddingIndexer/Dockerfile"
        });

        var taskDef = new FargateTaskDefinition(this, "task-def", new FargateTaskDefinitionProps
        {
            Family = "pokepad-embedding-indexer",
            Cpu = 256,
            MemoryLimitMiB = 512,
            ExecutionRole = executionRole,
            TaskRole = taskRole
        });

        taskDef.AddContainer("indexer", new ContainerDefinitionOptions
        {
            Image = ContainerImage.FromDockerImageAsset(image),
            Essential = true,
            Environment = new Dictionary<string, string>
            {
                { "SQS_QUEUE_URL", queue.QueueUrl },
                { "AI_API_KEY_PARAM", "/pokepad/ai-api-key" },
                { "VECTOR_DB_CONNECTION_STRING_PARAM", "/pokepad/vector-db-connection-string" },
                { "VECTOR_DB_SECRET_ARN", vectorStore.DbSecret.SecretArn }
            },
            Logging = LogDriver.AwsLogs(new AwsLogDriverProps
            {
                StreamPrefix = "pokepad-embedding-indexer",
                LogGroup = logGroup
            })
        });

        return taskDef;
    }

    private FargateService CreateService(Cluster cluster, FargateTaskDefinition taskDef, VectorStoreConstruct vectorStore)
    {
        return new FargateService(this, "indexer-service", new FargateServiceProps
        {
            ServiceName = "pokepad-embedding-indexer",
            Cluster = cluster,
            TaskDefinition = taskDef,
            DesiredCount = 0,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            SecurityGroups = [vectorStore.LambdaSecurityGroup],
            AssignPublicIp = false,
            // Allow scaling all the way to 0 without blocking deployments
            MinHealthyPercent = 0,
            MaxHealthyPercent = 100
        });
    }

    private void ScaleOnQueueDepth(FargateService service, Queue queue)
    {
        var scaling = service.AutoScaleTaskCount(new EnableScalingProps
        {
            MinCapacity = 0,
            MaxCapacity = 5
        });

        scaling.ScaleOnMetric("sqs-depth", new BasicStepScalingPolicyProps
        {
            Metric = queue.MetricApproximateNumberOfMessagesVisible(new Amazon.CDK.AWS.CloudWatch.MetricOptions
            {
                Period = Duration.Minutes(1),
                Statistic = "Maximum"
            }),
            ScalingSteps =
            [
                new ScalingInterval { Upper = 0, Change = 0 },   // empty → 0 workers
                new ScalingInterval { Lower = 1, Change = 1 },   // 1–9 messages → 1 worker
                new ScalingInterval { Lower = 10, Change = 3 },  // 10–49 messages → 3 workers
                new ScalingInterval { Lower = 50, Change = 5 }   // 50+ messages → 5 workers
            ],
            AdjustmentType = AdjustmentType.EXACT_CAPACITY,
            // Give workers time to finish the in-flight message before scaling in
            Cooldown = Duration.Minutes(5)
        });
    }

    private void CreateEventBridgeRule(DataLakeConstruct dataLake, Queue queue)
    {
        dataLake.Gold.EnableEventBridgeNotification();

        var rule = new Rule(this, "gold-put-rule", new RuleProps
        {
            RuleName = "pokepad-gold-object-created",
            EventPattern = new EventPattern
            {
                Source = ["aws.s3"],
                DetailType = ["Object Created"],
                Detail = new Dictionary<string, object>
                {
                    ["bucket"] = new Dictionary<string, object>
                    {
                        ["name"] = new[] { dataLake.Gold.BucketName }
                    },
                    ["object"] = new Dictionary<string, object>
                    {
                        // Only trigger on Parquet files landing in the products folder
                        ["key"] = new[] { new Dictionary<string, string> { ["prefix"] = "gold/products/" } }
                    }
                }
            }
        });

        // Send just the S3 detail (bucket + object) to SQS — workers read $.detail
        rule.AddTarget(new SqsQueue(queue, new SqsQueueProps
        {
            Message = RuleTargetInput.FromEventPath("$.detail")
        }));
    }
}
