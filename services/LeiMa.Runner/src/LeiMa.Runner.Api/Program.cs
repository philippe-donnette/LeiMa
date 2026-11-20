using LeiMa.Common.AspNetCore;
using LeiMa.Runner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddLeiMaDefaults();
builder.Services.AddRunnerInfrastructure();

var app = builder.Build();

app.UseLeiMaDefaults();

app.Run();
