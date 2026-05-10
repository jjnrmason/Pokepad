using Amazon.CDK;
using Constructs;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra;

public sealed class BuilderStack : Stack
{
    public BuilderStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        var dataLake = new DataLakeConstruct(this, "DataLake");
        var glueCatalog = new GlueCatalogConstruct(this, "GlueCatalog", dataLake.Gold);
        var cognito = new CognitoConstruct(this, "Cognito");
        var dynamo = new DynamoConstruct(this, "Dynamo");
        var vectorStore = new VectorStoreConstruct(this, "VectorStore");
        _ = new AthenaConstruct(this, "Athena", dataLake.AthenaResults);
        _ = new IamConstruct(this, "Iam", dataLake, glueCatalog);
        _ = new LambdaConstruct(this, "Lambda", dataLake, glueCatalog, cognito, dynamo, vectorStore);
        _ = new EcsConstruct(this, "Ecs", dataLake, vectorStore);
    }
}
