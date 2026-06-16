using JobScanner.Application;
using JobScanner.Infrastructure;
using JobScanner.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ScannerHostedService>();

var host = builder.Build();
host.Run();
