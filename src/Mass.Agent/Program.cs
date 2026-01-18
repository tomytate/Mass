using Mass.Agent;
using Mass.Core.Interfaces;
using Mass.Core.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ILogService, FileLogService>();
builder.Services.AddSingleton(_ => AgentConfiguration.LoadFromEnvironment());
builder.Services.AddHostedService<AgentWorker>();
builder.Services.AddHostedService<HeartbeatService>();

var host = builder.Build();
host.Run();
