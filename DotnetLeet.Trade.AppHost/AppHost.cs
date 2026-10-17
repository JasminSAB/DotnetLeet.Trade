var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL: one database, one schema per module. The volume keeps data between runs.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("dotnetleet-trade-postgres")
    .WithPgAdmin();

var tradeDb = postgres.AddDatabase("tradedb");

// Ollama: the local model behind the extraction port. The volume keeps the downloaded weights.
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("dotnetleet-trade-ollama");

// The full tag matters: the toolkit skips the download only when the local name matches exactly.
var extractionModel = ollama.AddModel("extraction-model", "llama3.2:latest");

var apiService = builder.AddProject<Projects.DotnetLeet_Trade_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(tradeDb)
    .WaitFor(tradeDb)
    .WithReference(extractionModel)
    .WaitFor(extractionModel);

builder.AddProject<Projects.DotnetLeet_Trade_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
