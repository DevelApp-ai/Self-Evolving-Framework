using System.Text.Json;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Security;

public sealed class DefaultRegoPolicyPackageTests
{
    [Fact]
    public void Default_Deny_List_Policy_File_Exists_With_Expected_Rules()
    {
        var policyPath = Path.Combine(GetRepositoryRoot(), "src", "SelfEvolvingFramework", "Security", "Policies", "default-deny.rego");

        Assert.True(File.Exists(policyPath));
        var policy = File.ReadAllText(policyPath);

        Assert.Contains("package selfevolving", policy, StringComparison.Ordinal);
        Assert.Contains("allow if count(deny) == 0", policy, StringComparison.Ordinal);

        foreach (var restrictedNamespace in new[]
                 {
                     "System.IO",
                     "System.Net",
                     "System.Reflection",
                     "System.Runtime.InteropServices"
                 })
        {
            Assert.Contains(restrictedNamespace, policy, StringComparison.Ordinal);
        }

        foreach (var restrictedInvocation in new[]
                 {
                     "System.IO.File",
                     "System.IO.Directory",
                     "System.Reflection.Assembly",
                     "System.Runtime.InteropServices.Marshal"
                 })
        {
            Assert.Contains(restrictedInvocation, policy, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Default_Policy_Fixtures_Are_Valid_And_Representative()
    {
        var fixturesPath = Path.Combine(GetRepositoryRoot(), "src", "SelfEvolvingFramework", "Security", "Policies", "fixtures");
        var allowFixturePath = Path.Combine(fixturesPath, "allow-input.json");
        var denyFixturePath = Path.Combine(fixturesPath, "deny-input.json");

        Assert.True(File.Exists(allowFixturePath));
        Assert.True(File.Exists(denyFixturePath));

        var allowInput = JsonSerializer.Deserialize<AstPolicyInput>(File.ReadAllText(allowFixturePath));
        var denyInput = JsonSerializer.Deserialize<AstPolicyInput>(File.ReadAllText(denyFixturePath));

        Assert.NotNull(allowInput);
        Assert.NotNull(denyInput);

        Assert.DoesNotContain("System.IO", allowInput.Namespaces);
        Assert.Contains("System.IO", denyInput.Namespaces);
        Assert.Contains("System.IO.File.ReadAllText", denyInput.MethodCalls);
    }

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
