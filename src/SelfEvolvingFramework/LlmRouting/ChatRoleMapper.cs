using Microsoft.SemanticKernel.ChatCompletion;

namespace SelfEvolvingFramework.LlmRouting;

internal static class ChatRoleMapper
{
    public static string ToApiRole(AuthorRole role)
        => role.Label switch
        {
            "system" => "system",
            "assistant" => "assistant",
            "tool" => "tool",
            _ => "user"
        };
}
