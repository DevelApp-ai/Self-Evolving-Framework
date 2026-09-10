using System.Text;
using System.Text.Json;

namespace SelfEvolvingFramework.Core;

internal static class LenientJson
{
    public static JsonDocument Parse(string json) => JsonDocument.Parse(Normalize(json));

    public static string Normalize(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return json ?? string.Empty;

        var builder = new StringBuilder(json.Length);
        var i = 0;
        while (i < json.Length)
        {
            var c = json[i];
            if (c == '"')
            {
                AppendVerbatimString(json, ref i, builder, '"');
            }
            else if (c == '\'')
            {
                AppendConvertedString(json, ref i, builder);
            }
            else
            {
                builder.Append(c);
                i++;
            }
        }
        return builder.ToString();
    }

    private static void AppendVerbatimString(string json, ref int i, StringBuilder builder, char quote)
    {
        builder.Append(quote);
        i++;
        while (i < json.Length)
        {
            var c = json[i];
            builder.Append(c);
            if (c == '\\' && i + 1 < json.Length)
            {
                i++;
                builder.Append(json[i]);
            }
            else if (c == quote)
            {
                i++;
                return;
            }
            else
            {
                i++;
            }
        }
    }

    private static void AppendConvertedString(string json, ref int i, StringBuilder builder)
    {
        builder.Append('"');
        i++;
        while (i < json.Length)
        {
            var c = json[i];
            if (c == '\\' && i + 1 < json.Length)
            {
                var next = json[i + 1];
                if (next == '\'')
                {
                    builder.Append('\'');
                    i += 2;
                }
                else
                {
                    builder.Append('\\');
                    builder.Append(next);
                    i += 2;
                }
            }
            else if (c == '\'')
            {
                builder.Append('"');
                i++;
                return;
            }
            else if (c == '"')
            {
                builder.Append('\\');
                builder.Append('"');
                i++;
            }
            else
            {
                builder.Append(c);
                i++;
            }
        }
        builder.Append('"');
    }
}
