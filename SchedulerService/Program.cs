using Scheduler.Application.Implementacion.Rabbit;
using Scheduler.Application.Implementacion.Scheduler;
using SchedulerService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IRabbitMqSubscriber, RabbitMqSubscriber>();
builder.Services.AddSingleton<ISftpProcessor, SftpFileProcessor>();
builder.Services.AddSingleton<IProcessFilesService, ProcessFilesService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
