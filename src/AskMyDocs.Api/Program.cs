using System.Text.Json.Serialization;
using AskMyDocs.Api.Features.Ask;
using AskMyDocs.Api.Features.Documents;
using AskMyDocs.Api.Infrastructure.AI;
using AskMyDocs.Api.Infrastructure.Persistence;
using AskMyDocs.Api.Ingestion;
using Scalar.AspNetCore;

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

// In Development the developer exception page (added automatically) returns the exception
// type, message and stack trace. Elsewhere, errors are reduced to a generic Problem Details.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Interactive API docs at /scalar, where you can upload PDFs and ask questions.
    app.MapScalarApiReference(options => options.WithTitle("AskMyDocs"));
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.MapDocumentEndpoints();
app.MapAskEndpoints();

app.Run();
