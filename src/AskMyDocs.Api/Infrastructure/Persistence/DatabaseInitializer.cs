using AskMyDocs.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AskMyDocs.Api.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (app.Environment.IsDevelopment())
        {
            await db.Database.MigrateAsync();
        }

        // The ingestion queue lives in memory, so anything still "Processing"
        // when the app starts was lost in a previous run.
        var interrupted = await db.Documents
            .Where(d => d.Status == DocumentStatus.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, DocumentStatus.Failed)
                .SetProperty(d => d.ErrorMessage, "Processing was interrupted by an application restart. Please upload the file again."));

        if (interrupted > 0)
        {
            app.Logger.LogWarning("Marked {Count} interrupted document(s) as Failed", interrupted);
        }
    }
}
