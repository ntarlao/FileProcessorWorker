using Scheduler.Application.Hubs;
using Scheduler.Application.Hubs.Interfaces;
using Scheduler.Application.Implementacion.Rabbit;
using Scheduler.Application.Implementacion.Scheduler;
using SchedulerService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IErrorNotifier, SignalRErrorNotifier>();

// en el pipeline (si tu worker expone endpoints HTTP) o en el builder si usas WebApplication:
builder.Services.AddSingleton<IArchivosService, ArchivosServices>();
builder.Services.AddSingleton<IRabbitMqSubscriber, RabbitMqSubscriber>();
builder.Services.AddSingleton<ISftpFileProcessor, SftpFileProcessor>();
builder.Services.AddHostedService<Worker>();
var app = builder.Build();

app.UseHttpsRedirection();
// Mapear hub SignalR
app.MapHub<ErrorHub>("/hubs/error");

app.Run();
