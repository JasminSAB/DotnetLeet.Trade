using DotnetLeet.Trade.ApiService;
using DotnetLeet.Trade.Infrastructure;
using DotnetLeet.Trade.Infrastructure.Persistence;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Aspire integrations: the database and the model. Both come from the AppHost.
builder.AddNpgsqlDbContext<TradeDbContext>("tradedb");
builder.AddOllamaApiClient("extraction-model").AddChatClient();

// A model call on a CPU takes 20 to 60 seconds. The standard resilience handler from
// ServiceDefaults cuts every HTTP attempt at 10 seconds and retries, which aborts the
// generation three times and then fails. One attempt, generous timeouts, no retry:
// a retried extraction is a second model call, and the idempotency belongs to the use case.
// The API's only outbound HTTP call is the model, so this applies to every pipeline in the API.
builder.Services.ConfigureAll<HttpStandardResilienceOptions>(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
    options.Retry.MaxRetryAttempts = 1;                      // the option rejects 0
    options.Retry.ShouldHandle = _ => PredicateResult.False(); // so no outcome ever qualifies for a retry
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(6);
});

// Ports wired to adapters.
builder.Services.AddTrade(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "DotnetLeet Trade API. See /openapi/v1.json.");

app.MapProcurementEndpoints();

app.MapDefaultEndpoints();

app.Run();
