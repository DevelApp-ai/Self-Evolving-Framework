namespace SelfEvolvingFramework.Tests.Integration;

public sealed class PublishWorkflowConfigurationTests
{
    [Fact]
    public void PublishWorkflow_Uses_Patch_Version_Bump_For_Release_And_Prerelease()
    {
        var workflow = ReadPublishWorkflow();

        Assert.Contains("base_version=\"$(bash scripts/compute-next-semver.sh)\"", workflow);
        Assert.Contains("PACKAGE_VERSION=$(bash scripts/compute-next-semver.sh)", workflow);
        Assert.DoesNotContain("SEMVER_BUMP=minor", workflow);
    }

    [Fact]
    public void PublishWorkflow_Resolves_NuGet_Key_From_Release_Environment_Context()
    {
        var workflow = ReadPublishWorkflow();

        Assert.Contains("environment: ${{ vars.RELEASE_ENVIRONMENT || 'production' }}", workflow);
        Assert.Contains("env:", workflow);
        Assert.Contains("NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}", workflow);
    }

    private static string ReadPublishWorkflow()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".github", "workflows", "publish-packages.yml");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate .github/workflows/publish-packages.yml from test runtime directory.");
    }
}
