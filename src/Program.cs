using DiscordBotService;
using DiscordBotService.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddNLog();
builder.Services.AddSecretsManager();
builder.Services.AddDiscordBot();
builder.Services.AddHostedService<WorkerService>();

var host = builder.Build();
host.Run();
host.Dispose();
