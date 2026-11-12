namespace SelfEvolvingFramework.Tests.Integration;

public sealed class PublishWorkflowConfigurationTests
{
    [Fact]
    public void PublishWorkflow_Uses_Minor_Version_Bump_For_Release_And_Prerelease()
    {
        var workflow = ReadPublishWorkflow();

        Assert.Contains("base_version=\"$(SEMVER_BUMP=minor bash scripts/compute-next-semver.sh)\"", workflow);
        Assert.Contains("PACKAGE_VERSION=$(SEMVER_BUMP=minor bash scripts/compute-next-semver.sh)", workflow);
        Assert.Contains("if [ \"${{ github.event_name }}\" = \"release\" ]; then", workflow);
        Assert.Contains("release_version=\"${release_tag#v}\"", workflow);
    }

    [Fact]
    public void PublishWorkflow_Resolves_NuGet_Key_From_Trusted_Publishing_Login()
    {
        var workflow = ReadPublishWorkflow();

        Assert.Contains("environment: ${{ vars.RELEASE_ENVIRONMENT || 'shared' }}", workflow);
        Assert.Contains("id-token: write", workflow);
        Assert.Contains("uses: NuGet/login@v1", workflow);
        Assert.Contains("user: ${{ secrets.NUGET_USER }}", workflow);
        Assert.Contains("dotnet nuget push \"${{ env.PACKAGE_FILE }}\" --source https://api.nuget.org/v3/index.json --api-key \"${{ steps.nuget-login.outputs.NUGET_API_KEY }}\" --skip-duplicate", workflow);
        Assert.DoesNotContain("secrets.NUGET_API_KEY", workflow);
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
