using AnzoneService;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "AnzoneService");
builder.Services.AddHostedService<EnforcementWorker>();
builder.Build().Run();
