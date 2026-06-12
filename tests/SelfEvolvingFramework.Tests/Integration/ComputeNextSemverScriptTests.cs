using System.Diagnostics;

namespace SelfEvolvingFramework.Tests.Integration;

public sealed class ComputeNextSemverScriptTests
{
    [Fact]
    public void ComputeNextSemver_Returns_Default_When_No_Tags()
    {
        var version = RunScriptWithTags(string.Empty);
        Assert.Equal("1.0.0", version);
    }

    [Fact]
    public void ComputeNextSemver_Uses_Highest_Stable_Tag_And_Increments_Patch()
    {
        const string tags = """
            0.9.0
            1.0.2
            1.0.10
            1.0.10-pr.1.2
            not-a-version
            1.0
            """;

        var version = RunScriptWithTags(tags);
        Assert.Equal("1.0.11", version);
    }

    [Fact]
    public void ComputeNextSemver_Increments_Minor_And_Resets_Patch_When_Configured()
    {
        const string tags = """
            1.0.2
            1.4.9
            """;

        var version = RunScriptWithTags(tags, semverBump: "minor");
        Assert.Equal("1.5.0", version);
    }

    [Fact]
    public void ComputeNextSemver_Increments_Major_And_Resets_Minor_And_Patch_When_Configured()
    {
        const string tags = """
            1.9.9
            2.3.4
            """;

        var version = RunScriptWithTags(tags, semverBump: "major");
        Assert.Equal("3.0.0", version);
    }

    private static string RunScriptWithTags(string tags, string semverBump = "patch")
    {
        var scriptPath = FindRepoScriptPath();
        var startInfo = new ProcessStartInfo("/usr/bin/env")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("bash");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.Environment["DEFAULT_VERSION"] = "1.0.0";
        startInfo.Environment["TAG_LIST"] = tags;
        startInfo.Environment["SEMVER_BUMP"] = semverBump;

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        process!.WaitForExit();

        var output = process.StandardOutput.ReadToEnd().Trim();
        var error = process.StandardError.ReadToEnd().Trim();

        Assert.True(process.ExitCode == 0, $"Expected success exit code but got {process.ExitCode}. stderr: {error}");
        return output;
    }

    private static string FindRepoScriptPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "scripts", "compute-next-semver.sh");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate scripts/compute-next-semver.sh from test runtime directory.");
    }
}
