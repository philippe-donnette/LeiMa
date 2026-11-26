# FAQ

Answers to questions and problems that come up while working on LeiMa locally. Add an entry when you solve something that cost you time. Keep answers short and say why the fix works.

## Dependencies and Dependabot

### Why does Dependabot not propose the 11.x versions of some packages?
**Short answer:** those packages moved to version 11 to match .NET 11, and LeiMa is on .NET 10. We closed the PRs and told Dependabot to ignore that major version. Minor and patch updates of the 10.x line still arrive.

**What was ignored** (closed with `@dependabot ignore this major version`):
- `Microsoft.Extensions.TimeProvider.Testing` 11.0.0 ([#62](https://github.com/philippe-donnette/LeiMa/pull/62)).
- `Serilog.AspNetCore` 11.0.0 ([#69](https://github.com/philippe-donnette/LeiMa/pull/69)).

**Why not just merge them:** the `LeiMa.Common*` packages are published and used by services that run on .NET 10. `Serilog.AspNetCore` 11.0.0 has a net10.0 build, but it needs `Serilog.Extensions.Hosting` 11.0.0, which in turn requires the 11.0.0 versions of `Microsoft.Extensions.DependencyInjection.Abstractions`, `Hosting.Abstractions` and `Logging.Abstractions` (checked in the package metadata on nuget.org). Every service would then get .NET 11 libraries next to a .NET 10 runtime and 10.x packages such as `Microsoft.AspNetCore.Authentication.JwtBearer`. That mix often works but is not a supported combination, and the CI of the Common library alone would not show a problem in a service. The 11.0.0 release notes of `TimeProvider.Testing` said it ships alongside .NET 11 and also carried draft and validation-not-passed comments. We gain nothing from either update today.

**See what is ignored:** comment on any Dependabot PR (or a closed one) `@dependabot show <package name> ignore conditions`. To undo an ignore, reopen the closed PR.

**Checklist for the move to .NET 11** (not planned yet):
1. Change `TargetFramework` in the `Directory.Build.props` files of `libs/dotnet/LeiMa.Common`, `services/LeiMa.Runner` and `aspire/LeiMa.AppHost`, and the .NET version in the `setup-dotnet` steps of `ci.yml` and `publish-common.yml`.
2. Reopen #62 and #69 (or raise the packages by hand) and bump the packages that follow .NET, such as `Microsoft.AspNetCore.*` and `Microsoft.Extensions.*`.
3. Run all tests, including the Docker ones, and start the AppHost.
4. Release the Common packages with a major version bump (the target framework changes for consumers), then move each service to the new version and framework.
This list is a starting point; revise it when the move is planned.

## GitHub token and secrets

### Restoring `LeiMa.Common*` fails with 401 Unauthorized, or Dependabot reports a feed authentication error
**Cause:** the GitHub personal access token (classic) with `read:packages` has expired or was revoked. The `LeiMa.Common*` packages are on GitHub Packages, which needs a token to restore even though the repository is public. (I have not reproduced the exact NuGet error text for an expired token; a 401 from the feed is the sign.)

**Check the token** without printing it. A valid token answers with `302` (a redirect to the package file), an invalid or expired one with `401`:
```bash
curl -s -o /dev/null -w "%{http_code}\n" -u "$LEIMA_NUGET_OWNER:$LEIMA_NUGET_TOKEN" "https://nuget.pkg.github.com/$LEIMA_NUGET_OWNER/download/leima.common/0.1.0/leima.common.0.1.0.nupkg"
```
Do not use the feed's `index.json` for this check: it answers `200` even with a wrong token.

**Fix: create a new token and update the two places that use it.**
1. Create it at https://github.com/settings/tokens (Developer settings, Personal access tokens, Tokens (classic), Generate new token (classic)). Name it for what it does (for example `leima-nuget-read`), pick an expiration, tick only `read:packages`, generate it and copy it. GitHub shows it once. Never paste it into a chat, a file in the repository or an issue.
2. Update your machine (PowerShell; the input is hidden):
   ```powershell
   $t = Read-Host 'Token' -AsSecureString; [Environment]::SetEnvironmentVariable('LEIMA_NUGET_TOKEN',[System.Net.NetworkCredential]::new('',$t).Password,'User')
   ```
   Then restart the terminal, the IDE and the Claude app so they pick up the new value.
3. Update the Dependabot secret in the repository (Settings, Secrets and variables, **Dependabot**, `LEIMA_NUGET_TOKEN`, Update), or from a terminal (it prompts for the value):
   ```bash
   gh secret set LEIMA_NUGET_TOKEN --app dependabot
   ```
4. Check again with the command above, then run `dotnet restore --force --no-cache` in `services/LeiMa.Runner`. Dependabot picks the new secret up on its next run; you can trigger one from the repository's Insights, Dependency graph, Dependabot page.

**What does not need changing:** CI and the release workflow use the built-in `GITHUB_TOKEN`, which GitHub creates for every run, so they never expire. `LEIMA_NUGET_OWNER` is your GitHub username and does not change. Pick an expiration you can live with and put a reminder in your calendar, because a classic token stops working silently on its expiry date.

### Which GitHub settings and secrets does LeiMa use?
| Where | Name | Purpose |
|---|---|---|
| Your machine (user environment variables) | `LEIMA_NUGET_OWNER`, `LEIMA_NUGET_TOKEN` | restore `LeiMa.Common*` from GitHub Packages through `services/nuget.config` |
| Repository, Dependabot secrets | `LEIMA_NUGET_OWNER`, `LEIMA_NUGET_TOKEN` | the same restore, for Dependabot's update jobs |
| GitHub Actions | `GITHUB_TOKEN` (automatic) | restore in CI (`packages: read`) and publish in the release workflow |

Dependabot secrets are separate from Actions secrets: a value set in one is not visible in the other.

## Local development (Aspire AppHost)

### The AppHost fails to start because the container name `leima-postgres` is already in use
**Symptom:** starting `aspire/LeiMa.AppHost` fails with a Docker error that the name `leima-postgres` is already taken.

**Cause:** the AppHost gives the PostgreSQL container the fixed name `leima-postgres`, so it is easy to find in Docker. Aspire normally removes the container when the AppHost stops. If the AppHost was killed or crashed, the old container stays, and Docker refuses a second container with the same name.

**Fix:**
```bash
docker rm -f leima-postgres
```
`-f` stops the container if it is still running and then removes it. The data is not lost: it lives in the Docker volume (name starts with `leima.apphost-`), which this command does not touch, so the next start reuses it.

### How do I reset the local database?
Remove the container and then the volume, so the next start creates an empty database:
```bash
docker rm -f leima-postgres
docker volume ls --filter name=leima.apphost
docker volume rm <volume name from the list>
```
This deletes all local job and task data. The volume name contains a hash, so list it first. The generated database password is kept in the AppHost user secrets and is reused, so you do not need to touch it.

### The AppHost reports `Container runtime 'docker' could not be found` or `context deadline exceeded`
**Cause:** Aspire checks the container runtime at startup, including `docker network ls`. When Docker Desktop's network component is stuck, that command hangs. Other commands such as `docker ps` can still work, which makes the problem confusing.

**Check:**
```bash
docker network ls
```
If this hangs and never prints a table, Docker Desktop is stuck.

**Fix:** restart Docker Desktop (this stops your other running containers) and start the AppHost again.

### Windows asks whether to trust a `localhost` certificate the first time I run the AppHost
**Cause:** `dotnet run` on the AppHost goes through the Aspire CLI, which trusts the ASP.NET Core HTTPS development certificate for your user account (the same as `dotnet dev-certs https --trust`). It only applies to `localhost` on your machine.

**Undo:**
```bash
dotnet dev-certs https --clean
```
The next run creates and trusts the certificate again.

### `dotnet run` on the AppHost fails with "cannot find LeiMa.AppHost.exe"
**Cause:** the Aspire CLI starts the Debug build. A Release-only build is not found.

**Fix:** build without `-c Release` (`dotnet build`), then run it again.

## Runner database and migrations

### The migrator logs two errors on its first run against an empty database
**Symptom:** the first start of `LeiMa.Runner.Migrator` on a new, empty database logs two `Failed executing DbCommand` errors for `__EFMigrationsHistory`, and then reports success and exits with code 0.

**Cause:** EF Core first looks for the migrations history table to see which migrations were applied. On a new database it does not exist yet, so PostgreSQL answers with an error, which EF logs and handles by creating the table. Nothing is wrong. The errors do not appear on later runs.

**What to do:** nothing, as long as the migrator ends with exit code 0 and the Api starts. If it does not, read the first error that is *not* about `__EFMigrationsHistory`.

### Can two migrators run at the same time?
Yes. EF Core 9 and later take a lock in the database before applying migrations, so concurrent runs wait for each other and each migration is applied once. This is checked against PostgreSQL with Npgsql by `MigrationTests` (three migrations at once leave one history row per migration). It matters when several instances of the Api or the migrator start together in another environment.

### `dotnet ef` fails with "The connection string 'runner' is not configured"
**Cause:** the tool builds the startup project (`LeiMa.Runner.Migrator`) to find the `DbContext`, and the Runner reads its connection string `runner` from configuration, which is not set when you run the tool by hand. The AppHost normally provides it.

**Fix:** set a placeholder; the tool does not connect for `migrations add` or `migrations list --no-connect`:
```bash
ConnectionStrings__runner="Host=localhost;Database=design_time_only" dotnet ef migrations list --no-connect \
  --project services/LeiMa.Runner/src/LeiMa.Runner.Infrastructure \
  --startup-project services/LeiMa.Runner/src/LeiMa.Runner.Migrator
```
In PowerShell, set the variable first: `$env:ConnectionStrings__runner = "Host=localhost;Database=design_time_only"`. The tool version is pinned in `dotnet-tools.json` (run `dotnet tool restore`).

### The integration tests are skipped on my machine
**Cause:** they need Docker (Testcontainers starts PostgreSQL). Locally they are skipped when Docker is not running; on CI they fail instead, so they are never silently skipped. Start Docker Desktop and run `dotnet test services/LeiMa.Runner/LeiMa.Runner.slnx` again. If `docker` hangs, see the Docker Desktop entry under Local development.

### Tasks come back in the wrong order after a database change
`GetAsync` orders the tasks by `Position` in the query. Without it, EF orders only by the job id and PostgreSQL happens to return the tasks in position order because of the unique `(JobId, Position)` index, which is not guaranteed. A test drops that index and reverses the stored positions to prove the explicit ordering works. Keep the `OrderBy` when changing the query.
