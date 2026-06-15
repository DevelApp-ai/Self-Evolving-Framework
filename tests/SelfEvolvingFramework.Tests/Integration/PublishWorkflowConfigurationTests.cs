namespace SelfEvolvingFramework.Tests.Integration;

public sealed class PublishWorkflowConfigurationTests
{
    [Fact]
    public void PublishWorkflow_Uses_Patch_Version_Bump_For_Release_And_Prerelease()
    {
        var workflow = ReadPublishWorkflow();

        Assert.Contains("base_version=\"$(bash scripts/compute-next-semver.sh)\"", workflow);
        Assert.Contains("PACKAGE_VERSION=$(bash scripts/compute-next-semver.sh)", workflow);
        Assert.Contains("if [ \"${{ github.event_name }}\" = \"release\" ]; then", workflow);
        Assert.Contains("release_version=\"${release_tag#v}\"", workflow);
        Assert.DoesNotContain("SEMVER_BUMP=minor", workflow);
    }

    [Fact]
    public void PublishWorkflow_Resolves_NuGet_Key_From_Release_Environment_Context()
    {
        var workflow = ReadPublishWorkflow();

        Assert.Contains("environment: ${{ vars.RELEASE_ENVIRONMENT || 'shared' }}", workflow);
        Assert.Contains("env:", workflow);
        Assert.Contains("NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}", workflow);
        Assert.Contains("NUGET_API_KEY is not configured. Add a valid NuGet.org API key in repository, organization, or selected release environment secrets", workflow);
    }

    [Fact]
    public void PublishWorkflow_Triggers_Release_Publishing_On_Release_Published()
    {
        var workflow = ReadPublishWorkflow();

        Assert.Contains("release:", workflow);
        Assert.Contains("- published", workflow);
        Assert.Contains("github.event_name == 'release' && github.event.action == 'published'", workflow);
    }

    [Fact]
    public void PublishWorkflow_Creates_GitHub_Release_Before_NuGet_Publish()
    {
        var workflow = ReadPublishWorkflow();
        var releaseStepIndex = workflow.IndexOf("      - name: Create or update GitHub Release", StringComparison.Ordinal);
        var nuGetPublishIndex = workflow.IndexOf("      - name: Publish release to NuGet.org", StringComparison.Ordinal);

        Assert.True(releaseStepIndex >= 0, "Expected Create or update GitHub Release step in publish workflow.");
        Assert.True(nuGetPublishIndex >= 0, "Expected Publish release to NuGet.org step in publish workflow.");
        Assert.True(releaseStepIndex < nuGetPublishIndex, "Expected GitHub Release step to run before NuGet.org publish step.");
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
