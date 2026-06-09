using System.Runtime.Loader;

namespace SelfEvolving.Framework.Execution;

internal sealed class IsolatedAssemblyLoadContext() : AssemblyLoadContext(name: $"evolution_{Guid.NewGuid():N}", isCollectible: true)
{
}
