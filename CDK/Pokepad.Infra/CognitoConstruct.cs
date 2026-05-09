using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Constructs;

namespace Pokepad.Infra;

public sealed class CognitoConstruct : Construct
{
    public UserPool UserPool { get; }
    public UserPoolClient UserPoolClient { get; }

    public CognitoConstruct(Construct scope, string id) : base(scope, id)
    {
        UserPool = new UserPool(this, "user-pool", new UserPoolProps
        {
            UserPoolName = "pokepad-users",
            SelfSignUpEnabled = false,
            SignInAliases = new SignInAliases { Email = true },
            PasswordPolicy = new PasswordPolicy
            {
                MinLength = 8,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireDigits = true,
                RequireSymbols = false
            },
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        UserPoolClient = UserPool.AddClient("app-client", new UserPoolClientOptions
        {
            UserPoolClientName = "pokepad-app-client",
            AuthFlows = new AuthFlow { UserPassword = true },
            GenerateSecret = false
        });

        new CfnUserPoolUser(this, "test-user", new CfnUserPoolUserProps
        {
            UserPoolId = UserPool.UserPoolId,
            Username = "test@pokepad.dev",
            MessageAction = "SUPPRESS",
            UserAttributes = new[]
            {
                new CfnUserPoolUser.AttributeTypeProperty { Name = "email", Value = "test@pokepad.dev" },
                new CfnUserPoolUser.AttributeTypeProperty { Name = "email_verified", Value = "true" }
            }
        });

        _ = new CfnOutput(this, "UserPoolId", new CfnOutputProps
        {
            Value = UserPool.UserPoolId,
            Description = "Cognito User Pool ID"
        });

        _ = new CfnOutput(this, "UserPoolClientId", new CfnOutputProps
        {
            Value = UserPoolClient.UserPoolClientId,
            Description = "Cognito App Client ID"
        });
    }
}
