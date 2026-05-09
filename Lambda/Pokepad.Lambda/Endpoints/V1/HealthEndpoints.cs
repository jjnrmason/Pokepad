namespace Pokepad.Lambda.Endpoints.V1;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
            .WithName("Health")
            .WithSummary("Health check")
            .WithDescription("Returns 200 OK when the service is running. No authentication required.")
            .WithTags("Health");
    }
}
