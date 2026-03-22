using AlertHub.Api.EndPoints;


namespace AlertHub.Api;

public static class ApiStartup
{
    public static void MapEndpoints(this WebApplication app)
    {
        app.MapIncidentEndpoints();
        app.MapServiceEndpoints();
    }
}
