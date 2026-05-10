using NUnit.Framework;

namespace Pokepad.Infra.Tests.LambdaStack.WhenWorkingWithTheLambdaConstruct;

public partial class WhenWorkingWithTheLambdaConstruct
{
    public class AndCheckingTheHttpApi : LambdaConstructTestBase
    {
        [Test]
        public void ThenOneHttpApiIsCreated()
        {
            Assert.That(this.Template.FindResources("AWS::ApiGatewayV2::Api"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheApiNameIsPokepadSearchApi()
        {
            var apis = this.Template.FindResources("AWS::ApiGatewayV2::Api");
            var props = (IDictionary<string, object>)apis.Values.Single()["Properties"];
            Assert.That(props["Name"]?.ToString(), Is.EqualTo("pokepad-search-api"));
        }

        [Test]
        public void ThenCorsIsConfigured()
        {
            var apis = this.Template.FindResources("AWS::ApiGatewayV2::Api");
            var props = (IDictionary<string, object>)apis.Values.Single()["Properties"];
            Assert.That(props.ContainsKey("CorsConfiguration"), Is.True);
        }

        [Test]
        public void ThenTheSearchRouteExists()
        {
            var routes = this.Template.FindResources("AWS::ApiGatewayV2::Route");
            var hasSearchRoute = routes.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["RouteKey"]?.ToString() == "POST /v1/search";
            });
            Assert.That(hasSearchRoute, Is.True);
        }

        [Test]
        public void ThenTheHealthRouteExists()
        {
            var routes = this.Template.FindResources("AWS::ApiGatewayV2::Route");
            var hasHealthRoute = routes.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["RouteKey"]?.ToString() == "GET /v1/health";
            });
            Assert.That(hasHealthRoute, Is.True);
        }

        [Test]
        public void ThenTheQueryStartRouteExists()
        {
            var routes = this.Template.FindResources("AWS::ApiGatewayV2::Route");
            var hasRoute = routes.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["RouteKey"]?.ToString() == "POST /v1/query/start";
            });
            Assert.That(hasRoute, Is.True);
        }

        [Test]
        public void ThenTheQueryStatusRouteExists()
        {
            var routes = this.Template.FindResources("AWS::ApiGatewayV2::Route");
            var hasRoute = routes.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["RouteKey"]?.ToString() == "GET /v1/query/{id}/status";
            });
            Assert.That(hasRoute, Is.True);
        }

        [Test]
        public void ThenTheQueryResultsRouteExists()
        {
            var routes = this.Template.FindResources("AWS::ApiGatewayV2::Route");
            var hasRoute = routes.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["RouteKey"]?.ToString() == "GET /v1/query/{id}/results";
            });
            Assert.That(hasRoute, Is.True);
        }

        [Test]
        public void ThenTheCognitoAuthorizerIsAttachedToTheSearchRoute()
        {
            var routes = this.Template.FindResources("AWS::ApiGatewayV2::Route");
            var searchRoute = routes.Values.First(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["RouteKey"]?.ToString() == "POST /v1/search";
            });
            var routeProps = (IDictionary<string, object>)searchRoute["Properties"];
            Assert.That(routeProps.ContainsKey("AuthorizerId"), Is.True);
        }

        [Test]
        public void ThenTheHealthRouteHasNoAuthorizer()
        {
            var routes = this.Template.FindResources("AWS::ApiGatewayV2::Route");
            var healthRoute = routes.Values.First(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["RouteKey"]?.ToString() == "GET /v1/health";
            });
            var routeProps = (IDictionary<string, object>)healthRoute["Properties"];
            var hasAuth = routeProps.TryGetValue("AuthorizationType", out var authType)
                && authType?.ToString() != "NONE";
            Assert.That(hasAuth, Is.False);
        }

        [Test]
        public void ThenTheSearchApiUrlOutputIsCreated()
        {
            var outputs = this.Template.FindOutputs("*");
            Assert.That(outputs.Keys.Any(k => k.Contains("SearchApiUrl")), Is.True);
        }
    }
}
