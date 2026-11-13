# LeiMa.Common.AspNetCore

Standard ASP.NET Core setup for LeiMa services.

```csharp
builder.AddLeiMaDefaults();
var app = builder.Build();
app.UseLeiMaDefaults();
```

- Serilog logging (readable console in Development, compact JSON otherwise)
- OpenTelemetry traces and metrics (OTLP export when `OTEL_EXPORTER_OTLP_ENDPOINT` is set)
- ProblemDetails responses and a global exception handler
- `X-Correlation-Id` middleware
- `/health/live` and `/health/ready`
- `AddValidatedOptions<T>`, JWT bearer wiring and a deny-by-default authorization policy
- `Result` to HTTP mapping

Part of the LeiMa.Common packages, which share one version. Cross-cutting infrastructure only. See the repository's `libs/dotnet/LeiMa.Common/CLAUDE.md` for details.
