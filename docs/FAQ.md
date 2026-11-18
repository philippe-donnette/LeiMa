# FAQ

Answers to questions and problems that come up while working on LeiMa locally. Add an entry when you solve something that cost you time. Keep answers short and say why the fix works.

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
