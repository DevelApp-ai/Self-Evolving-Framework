using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SelfEvolvingFramework.LlmRouting;

public sealed class RuntimeSandboxExecutor : IRuntimeSandboxExecutor
{
    public async Task<int> ExecuteShellAsync(string command, SandboxOptions options, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command must be provided.", nameof(command));
        }

        ArgumentNullException.ThrowIfNull(options);

        if (IsHostExecutor(options.ExecutorType) && IsProductionEnvironment())
        {
            throw new InvalidOperationException("Host command execution is blocked in production mode. Configure a sandbox executor.");
        }

        if (!IsHostExecutor(options.ExecutorType))
        {
            throw new NotSupportedException($"Executor type '{options.ExecutorType}' is not supported by this runtime executor.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(options.TimeoutMilliseconds));

        var startInfo = BuildShellStartInfo(command);
        using var process = new Process { StartInfo = startInfo };
        process.Start();

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException($"Sandbox command exceeded timeout of {options.TimeoutMilliseconds}ms.");
        }

        return process.ExitCode;
    }

    private static ProcessStartInfo BuildShellStartInfo(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
        }

        return new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    private static bool IsHostExecutor(string executorType)
        => string.Equals(executorType, "host", StringComparison.OrdinalIgnoreCase);

    private static bool IsProductionEnvironment()
    {
        var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (string.Equals(dotnetEnvironment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(aspNetCoreEnvironment, "Production", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}
