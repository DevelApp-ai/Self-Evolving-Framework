using SelfEvolving.Framework.Security;

namespace SelfEvolving.Framework.Tests.Security;

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
}
