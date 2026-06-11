using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Security;

public sealed class RoslynAstSecurityEvaluatorTests
{
    [Fact]
    public void Evaluate_Allows_Safe_Code()
    {
        var evaluator = new RoslynAstSecurityEvaluator();
        const string source = "public static class Sample { public static int Run() => 1; }";

        var result = evaluator.Evaluate(source);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Evaluate_Blocks_Restricted_Namespace()
    {
        var evaluator = new RoslynAstSecurityEvaluator();
        const string source = "using System.IO; public static class Sample { public static int Run() => 1; }";

        var result = evaluator.Evaluate(source);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Contains("System.IO", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_Blocks_Restricted_Invocation()
    {
        var evaluator = new RoslynAstSecurityEvaluator();
        const string source = "public static class Sample { public static string Run() => System.IO.File.ReadAllText(\"x\"); }";

        var result = evaluator.Evaluate(source);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Contains("Restricted invocation", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_Blocks_Global_Qualified_Restricted_Invocation()
    {
        var evaluator = new RoslynAstSecurityEvaluator();
        const string source = "public static class Sample { public static string Run() => global::System.IO.File.ReadAllText(\"x\"); }";

        var result = evaluator.Evaluate(source);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Contains("Restricted invocation", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_Blocks_Restricted_Object_Creation()
    {
        var evaluator = new RoslynAstSecurityEvaluator();
        const string source = "public static class Sample { public static object Run() => new System.IO.FileInfo(\"x\"); }";

        var result = evaluator.Evaluate(source);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Contains("Restricted type", StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_Respects_Custom_Restricted_Options()
    {
        var options = new AstSecurityOptions();
        options.RestrictedNamespaces.Clear();
        options.RestrictedInvocations.Clear();
        options.RestrictedNamespaces.Add("System.Text");

        var evaluator = new RoslynAstSecurityEvaluator(options);
        const string source = "using System.Text; public static class Sample { public static int Run() => 1; }";

        var result = evaluator.Evaluate(source);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Violations, v => v.Contains("System.Text", StringComparison.Ordinal));
    }
}
