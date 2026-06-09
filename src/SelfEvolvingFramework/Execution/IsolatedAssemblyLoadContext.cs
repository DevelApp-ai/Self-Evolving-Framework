using System.Runtime.Loader;

namespace SelfEvolvingFramework.Execution;

internal sealed class IsolatedAssemblyLoadContext() : AssemblyLoadContext(name: $"evolution_{Guid.NewGuid():N}", isCollectible: true)
{
}
