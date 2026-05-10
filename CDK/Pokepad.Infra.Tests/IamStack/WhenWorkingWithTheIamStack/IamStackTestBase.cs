using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.IamStack.WhenWorkingWithTheIamStack;

public class IamStackTestBase
{
    protected IamConstruct IamConstruct { get; private set; } = null!;
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        var dataLake = new DataLakeConstruct(stack, "DataLake");
        var glueCatalog = new GlueCatalogConstruct(stack, "GlueCatalog", dataLake.Gold);
        this.IamConstruct = new IamConstruct(stack, "Iam", dataLake, glueCatalog);
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }

    protected IDictionary<string, object>? FindRoleByName(string roleName) =>
        this.Template.FindResources("AWS::IAM::Role").Values
            .FirstOrDefault(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props.TryGetValue("RoleName", out var name) && name?.ToString() == roleName;
            });

    protected string? GetServicePrincipal(IDictionary<string, object> role)
    {
        var props = (IDictionary<string, object>)role["Properties"];
        var doc = (IDictionary<string, object>)props["AssumeRolePolicyDocument"];
        var statements = (IList<object>)doc["Statement"];
        foreach (var rawStmt in statements)
        {
            var stmt = (IDictionary<string, object>)rawStmt;
            if (!stmt.TryGetValue("Principal", out var rawPrincipal)) continue;
            var principal = (IDictionary<string, object>)rawPrincipal;
            if (principal.TryGetValue("Service", out var service))
                return service?.ToString();
        }
        return null;
    }

    protected bool HasAwsPrincipal(IDictionary<string, object> role)
    {
        var props = (IDictionary<string, object>)role["Properties"];
        var doc = (IDictionary<string, object>)props["AssumeRolePolicyDocument"];
        var statements = (IList<object>)doc["Statement"];
        return statements.Cast<IDictionary<string, object>>().Any(stmt =>
        {
            if (!stmt.TryGetValue("Principal", out var rawPrincipal)) return false;
            var principal = (IDictionary<string, object>)rawPrincipal;
            return principal.ContainsKey("AWS");
        });
    }

    protected bool AnyPolicyStatementContainsAction(string action) =>
        this.Template.FindResources("AWS::IAM::Policy").Values.Any(resource =>
        {
            var props = (IDictionary<string, object>)resource["Properties"];
            var doc = (IDictionary<string, object>)props["PolicyDocument"];
            var statements = (IList<object>)doc["Statement"];
            return statements.Cast<IDictionary<string, object>>().Any(stmt =>
            {
                if (!stmt.TryGetValue("Action", out var rawActions)) return false;
                return rawActions is string s ? s == action
                    : rawActions is IList<object> list && list.Any(a => a?.ToString() == action);
            });
        });
}
