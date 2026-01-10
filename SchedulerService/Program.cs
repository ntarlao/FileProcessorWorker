using Scheduler.Application.Hubs.Interfaces;
using Scheduler.Application.Implementacion.Rabbit;
using Scheduler.Application.Implementacion.Scheduler;
using SchedulerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddSingleton<IErrorNotifier, SignalRErrorNotifier>();

builder.Services.AddSingleton<IArchivosService, ArchivosServices>();
builder.Services.AddSingleton<IRabbitMqSubscriber, RabbitMqSubscriber>();
builder.Services.AddSingleton<ISftpFileProcessor, SftpFileProcessor>();
//builder.Services.AddHostedService<Worker>();

var app = builder.Build();
app.Run();
