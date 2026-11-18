# FAQ

Answers to questions and problems that come up while working on LeiMa locally. Add an entry when you solve something that cost you time. Keep answers short and say why the fix works.

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
