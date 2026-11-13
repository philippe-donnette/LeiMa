using System.Diagnostics;

namespace LeiMa.Common.Testing;

/// <summary>Detects whether a Docker daemon is reachable. The result is cached for the test run.</summary>
public static class DockerAvailability
{
    private static readonly Lazy<bool> Detected = new(Detect);

    /// <summary>True when <c>docker info</c> succeeds.</summary>
    public static bool IsAvailable => Detected.Value;

    private static bool Detect()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(TimeSpan.FromSeconds(15)))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// A <see cref="FactAttribute"/> for tests that need Docker (Testcontainers). The test is skipped when Docker is not
/// available, except on CI (<c>CI</c> or <c>GITHUB_ACTIONS</c> set), where it fails so missing Docker cannot hide untested code.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    /// <summary>Sets <see cref="FactAttribute.Skip"/> when Docker is missing outside CI.</summary>
    public RequiresDockerFactAttribute()
    {
        if (!IsCi() && !DockerAvailability.IsAvailable)
        {
            Skip = "Docker is not available.";
        }
    }

    private static bool IsCi() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
        || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
}
