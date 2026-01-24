var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.PokManager_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.PokManager_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
