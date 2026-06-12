using System.Text.Json;
using SelfEvolvingFramework.Security;

namespace SelfEvolvingFramework.Tests.Security;

public sealed class RoslynAstPolicyInputSerializerTests
{
    [Fact]
    public void Create_Extracts_Deduplicated_And_Normalized_Ast_Data()
    {
        var serializer = new RoslynAstPolicyInputSerializer();
        const string source = """
                              using System;
                              using global::System.IO;
                              using System.IO;

                              public static class Sample
                              {
                                  public static void Run()
                                  {
                                      global::System.Console.WriteLine("x");
                                      System.Console.WriteLine("y");
                                      var info = new global::System.IO.FileInfo("x");
                                  }
                              }
                              """;

        var input = serializer.Create(source);

        Assert.Equal(["System", "System.IO"], input.Namespaces);
        Assert.Equal(["System.Console.WriteLine"], input.MethodCalls);
        Assert.Equal(["System.IO.FileInfo"], input.ObjectCreations);
    }

    [Fact]
    public void Serialize_Produces_Json_With_Policy_Input_Properties()
    {
        var serializer = new RoslynAstPolicyInputSerializer();
        const string source = "using System.Text; public static class Sample { public static object Run() => new System.Text.StringBuilder(); }";

        var json = serializer.Serialize(source);
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        Assert.True(root.TryGetProperty("Namespaces", out var namespaces));
        Assert.True(root.TryGetProperty("MethodCalls", out var methodCalls));
        Assert.True(root.TryGetProperty("ObjectCreations", out var objectCreations));

        Assert.Contains(namespaces.EnumerateArray().Select(element => element.GetString()), value => value == "System.Text");
        Assert.Empty(methodCalls.EnumerateArray());
        Assert.Contains(objectCreations.EnumerateArray().Select(element => element.GetString()), value => value == "System.Text.StringBuilder");
    }
}

