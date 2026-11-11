namespace AskMyDocs.Api.Ingestion;

public static class IngestionExtensions
{
    public static IServiceCollection AddIngestion(this IServiceCollection services)
    {
        services.AddSingleton<IngestionQueue>();
        services.AddHostedService<IngestionWorker>();
        return services;
    }
}
