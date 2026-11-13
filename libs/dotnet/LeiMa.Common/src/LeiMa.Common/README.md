# LeiMa.Common

Core helpers shared by LeiMa .NET services. No heavy dependencies.

- `Result` / `Result<T>` / `Error` (typed by `ErrorType`) for expected failures
- `Ids.NewId()`: time-ordered GUIDs (version 7)
- `TimeProvider` helpers: inject `TimeProvider` instead of `DateTime.UtcNow`
- `CorrelationConstants`: the correlation header name and log property

Part of the LeiMa.Common packages (`LeiMa.Common.AspNetCore`, `LeiMa.Common.EntityFrameworkCore`, `LeiMa.Common.Testing`), which share one version. Cross-cutting infrastructure only: no domain models, business logic or API contracts.

The packages are published to GitHub Packages. See the repository's `libs/dotnet/LeiMa.Common/CLAUDE.md` for usage and how to consume them.
