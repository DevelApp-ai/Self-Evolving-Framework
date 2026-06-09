namespace SelfEvolving.Framework.Security;

public sealed class AstSecurityOptions
{
    public ISet<string> RestrictedNamespaces { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "System.IO",
        "System.Net",
        "System.Reflection",
        "System.Runtime.InteropServices"
    };

    public ISet<string> RestrictedInvocations { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "System.IO.File",
        "System.IO.Directory",
        "System.Reflection.Assembly",
        "System.Runtime.InteropServices.Marshal"
    };
}
