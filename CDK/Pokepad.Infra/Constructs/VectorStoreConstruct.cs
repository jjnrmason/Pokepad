using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SSM;
using Amazon.CDK.CustomResources;
using Constructs;

namespace Pokepad.Infra.Constructs;

public sealed class VectorStoreConstruct : Construct
{
    public Vpc Vpc { get; }
    public SecurityGroup LambdaSecurityGroup { get; }
    public IStringParameter ConnectionStringParameter { get; }
    public Amazon.CDK.AWS.SecretsManager.ISecret DbSecret { get; }

    public VectorStoreConstruct(Construct scope, string id, bool multiAz = false) : base(scope, id)
    {
        Vpc = CreateVpc();
        LambdaSecurityGroup = CreateLambdaSecurityGroup(Vpc);
        var rdsSecurityGroup = CreateRdsSecurityGroup(Vpc, LambdaSecurityGroup);
        var instance = CreateRdsInstance(Vpc, rdsSecurityGroup, multiAz);
        DbSecret = instance.Secret!;
        ConnectionStringParameter = CreateConnectionStringParameter(instance);
        InitializeDatabase(instance, Vpc, LambdaSecurityGroup);
        CreateBastion(Vpc, rdsSecurityGroup, instance);
    }

    private Vpc CreateVpc()
    {
        var vpc = new Vpc(this, "vpc", new VpcProps
        {
            VpcName = "pokepad-vpc",
            MaxAzs = 2,
            NatGatewayProvider = NatProvider.InstanceV2(new NatInstanceProps
            {
                InstanceType = new Amazon.CDK.AWS.EC2.InstanceType("t4g.nano")
            }),
            NatGateways = 1,
            SubnetConfiguration =
            [
                new SubnetConfiguration
                {
                    Name = "public",
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = 24
                },
                new SubnetConfiguration
                {
                    Name = "private",
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                    CidrMask = 24
                },
                new SubnetConfiguration
                {
                    Name = "isolated",
                    SubnetType = SubnetType.PRIVATE_ISOLATED,
                    CidrMask = 28
                }
            ]
        });

        // Free gateway endpoint — S3 traffic stays on the AWS backbone, bypasses the NAT instance
        vpc.AddGatewayEndpoint("s3-endpoint", new GatewayVpcEndpointOptions
        {
            Service = GatewayVpcEndpointAwsService.S3
        });

        return vpc;
    }

    private SecurityGroup CreateLambdaSecurityGroup(Vpc vpc)
    {
        return new SecurityGroup(this, "lambda-sg", new SecurityGroupProps
        {
            SecurityGroupName = "pokepad-lambda-sg",
            Vpc = vpc,
            Description = "Security group for Pokepad Lambda functions",
            AllowAllOutbound = true
        });
    }

    private SecurityGroup CreateRdsSecurityGroup(Vpc vpc, SecurityGroup lambdaSg)
    {
        var sg = new SecurityGroup(this, "rds-sg", new SecurityGroupProps
        {
            SecurityGroupName = "pokepad-rds-sg",
            Vpc = vpc,
            Description = "Security group for Pokepad RDS vector store with inbound 5432 from Lambda only",
            AllowAllOutbound = false
        });

        sg.AddIngressRule(lambdaSg, Port.Tcp(5432), "Allow inbound PostgreSQL from Lambda security group");

        return sg;
    }

    private DatabaseInstance CreateRdsInstance(Vpc vpc, SecurityGroup sg, bool multiAz)
    {
        return new DatabaseInstance(this, "rds", new DatabaseInstanceProps
        {
            InstanceIdentifier = "pokepad-vector-store",
            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps
            {
                Version = PostgresEngineVersion.VER_16
            }),
            InstanceType = new Amazon.CDK.AWS.EC2.InstanceType("t3.micro"),
            DatabaseName = "pokepad",
            Credentials = Credentials.FromGeneratedSecret("pokepad", new CredentialsFromUsernameOptions
            {
                SecretName = "pokepad/vector-db-credentials"
            }),
            MultiAz = multiAz,
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
            SecurityGroups = [sg],
            PubliclyAccessible = false,
            StorageEncrypted = true,
            BackupRetention = Duration.Days(7),
            RemovalPolicy = RemovalPolicy.DESTROY,
            DeletionProtection = false,
            EnablePerformanceInsights = false
        });
    }

    private StringParameter CreateConnectionStringParameter(DatabaseInstance instance)
    {
        return new StringParameter(this, "connection-string-param", new StringParameterProps
        {
            ParameterName = "/pokepad/vector-db-connection-string",
            StringValue = $"Host={instance.DbInstanceEndpointAddress};Port={instance.DbInstanceEndpointPort};Database=pokepad;Username=pokepad",
            Description = "Pokepad vector store connection string with password in Secrets Manager at pokepad/vector-db-credentials"
        });
    }

    private void CreateBastion(Vpc vpc, SecurityGroup rdsSg, DatabaseInstance instance)
    {
        var bastionSg = new SecurityGroup(this, "bastion-sg", new SecurityGroupProps
        {
            SecurityGroupName = "pokepad-bastion-sg",
            Vpc = vpc,
            Description = "Security group for RDS bastion no inbound, outbound PostgreSQL to RDS only",
            AllowAllOutbound = false
        });

        bastionSg.AddEgressRule(Peer.AnyIpv4(), Port.Tcp(443), "Allow outbound HTTPS for SSM agent");
        bastionSg.AddEgressRule(rdsSg, Port.Tcp(5432), "Allow outbound PostgreSQL to RDS");
        rdsSg.AddIngressRule(bastionSg, Port.Tcp(5432), "Allow inbound PostgreSQL from bastion");

        var bastionRole = new Role(this, "bastion-role", new RoleProps
        {
            AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
            ManagedPolicies =
            [
                ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore")
            ]
        });

        var bastion = new Instance_(this, "bastion", new Amazon.CDK.AWS.EC2.InstanceProps
        {
            InstanceName = "pokepad-bastion",
            InstanceType = new Amazon.CDK.AWS.EC2.InstanceType("t4g.nano"),
            MachineImage = MachineImage.LatestAmazonLinux2023(new AmazonLinux2023ImageSsmParameterProps
            {
                CpuType = AmazonLinuxCpuType.ARM_64
            }),
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            SecurityGroup = bastionSg,
            Role = bastionRole,
            // No key pair — access is via SSM only
        });

        _ = new CfnOutput(this, "BastionInstanceId", new CfnOutputProps
        {
            Value = bastion.InstanceId,
            Description = "Bastion instance ID use with SSM port forwarding to reach RDS"
        });

        _ = new CfnOutput(this, "RdsEndpoint", new CfnOutputProps
        {
            Value = instance.DbInstanceEndpointAddress,
            Description = "RDS endpoint hostname"
        });
    }

    private void InitializeDatabase(DatabaseInstance instance, Vpc vpc, SecurityGroup lambdaSg)
    {
        var initRole = new Role(this, "db-init-role", new RoleProps
        {
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            ManagedPolicies =
            [
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaVPCAccessExecutionRole")
            ]
        });

        initRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["secretsmanager:GetSecretValue"],
            Resources = [instance.Secret!.SecretArn]
        }));

        var initFunction = new Function(this, "db-init-fn", new FunctionProps
        {
            Runtime = Runtime.PYTHON_3_12,
            Handler = "handler.on_event",
            Code = Code.FromAsset(
                "db-init-handler",
                new Amazon.CDK.AWS.S3.Assets.AssetOptions
                {
                    Bundling = new BundlingOptions
                    {
                        Image = DockerImage.FromRegistry("python:3.12-slim"),
                        Command =
                        [
                            "bash", "-c",
                            "pip install -r requirements.txt -t /asset-output && cp handler.py /asset-output/"
                        ]
                    }
                }),
            Role = initRole,
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            SecurityGroups = [lambdaSg],
            Timeout = Duration.Minutes(5)
        });

        var provider = new Provider(this, "db-init-provider", new ProviderProps
        {
            OnEventHandler = initFunction
        });

        _ = new CustomResource(this, "db-init", new CustomResourceProps
        {
            ServiceToken = provider.ServiceToken,
            Properties = new Dictionary<string, object>
            {
                { "Host", instance.DbInstanceEndpointAddress },
                { "Port", instance.DbInstanceEndpointPort },
                { "Database", "pokepad" },
                { "SecretArn", instance.Secret.SecretArn }
            }
        });
    }
}
