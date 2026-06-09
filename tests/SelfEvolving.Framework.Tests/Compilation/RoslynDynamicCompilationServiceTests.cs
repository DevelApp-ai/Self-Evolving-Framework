using SelfEvolving.Framework.Compilation;

namespace SelfEvolving.Framework.Tests.Compilation;

public sealed class RoslynDynamicCompilationServiceTests
{
    [Fact]
    public void Compile_Returns_Assembly_For_Valid_Code()
    {
        var compiler = new RoslynDynamicCompilationService();
        const string source = "public static class Runner { public static int Execute() => 42; }";

        var result = compiler.Compile(source);

        Assert.True(result.Success);
        Assert.NotNull(result.AssemblyBytes);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Compile_Returns_Diagnostics_For_Invalid_Code()
    {
        var compiler = new RoslynDynamicCompilationService();
        const string source = "public static class Runner { public static int Execute( => 42; }";

        var result = compiler.Compile(source);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
    }
}
