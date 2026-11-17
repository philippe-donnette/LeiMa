using LeiMa.Common.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddLeiMaDefaults();

var app = builder.Build();

app.UseLeiMaDefaults();

app.Run();
