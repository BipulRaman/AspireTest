var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireApp1_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AspireApp1_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

var azFunction = builder.AddAzureFunctionsProject<Projects.AspireApp1_AzFunction>("aspireapp1-azfunction")
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:7071")
    .WithHttpHealthCheck("/api/health");

builder.Build().Run();
