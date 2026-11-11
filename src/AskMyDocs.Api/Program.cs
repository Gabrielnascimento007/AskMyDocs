using System.Text.Json.Serialization;
using AskMyDocs.Api.Features.Ask;
using AskMyDocs.Api.Features.Documents;
using AskMyDocs.Api.Infrastructure.AI;
using AskMyDocs.Api.Infrastructure.Persistence;
using AskMyDocs.Api.Ingestion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddPersistence(builder.Configuration)
    .AddOpenAi(builder.Configuration)
    .AddIngestion();

var app = builder.Build();

await app.InitializeDatabaseAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapDocumentEndpoints();
app.MapAskEndpoints();

app.Run();
