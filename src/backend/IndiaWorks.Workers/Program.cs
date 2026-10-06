using IndiaWorks.Application;
using IndiaWorks.Infrastructure;
using IndiaWorks.Workers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddIndiaWorksApplication();
builder.Services.AddIndiaWorksInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
