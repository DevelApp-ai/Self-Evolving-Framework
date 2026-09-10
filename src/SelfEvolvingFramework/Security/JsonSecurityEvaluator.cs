using System.Text;
using System.Text.Json;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Security;

public sealed class JsonSecurityEvaluator : IAstSecurityEvaluator
{
    private readonly JsonSecurityOptions _options;

    public CandidateFormat Format => CandidateFormat.Json;

    public JsonSecurityEvaluator(JsonSecurityOptions? options = null)
    {
        _options = options ?? new();
    }

    public SecurityEvaluationResult Evaluate(string sourceMaterial)
    {
        try
        {
            var violations = new List<string>();

            using var jsonDoc = LenientJson.Parse(sourceMaterial);

            if (ContainsPrototypePollution(jsonDoc))
                violations.Add("Potential prototype pollution pattern detected");

            if (ContainsCircularReferences(jsonDoc, _options.MaxDepth))
                violations.Add("Circular reference detected or JSON too deeply nested");

            if (ExceedsSizeLimit(sourceMaterial, _options.MaxSizeBytes))
                violations.Add("JSON exceeds maximum size limit");

            if (ContainsDisallowedPatterns(jsonDoc, _options.DisallowedPatterns))
                violations.Add("Disallowed pattern detected in JSON");

            if (_options.RequiredFields != null && _options.RequiredFields.Count > 0)
            {
                var missingFields = CheckRequiredFields(jsonDoc, _options.RequiredFields);
                if (missingFields.Count > 0)
                {
                    violations.Add($"Missing required fields: {string.Join(", ", missingFields)}");
                }
            }

            return new SecurityEvaluationResult(violations.Count == 0, violations);
        }
        catch (JsonException ex)
        {
            return new SecurityEvaluationResult(false, new[] { $"Invalid JSON: {ex.Message}" });
        }
    }

    private static bool ContainsPrototypePollution(JsonDocument jsonDoc)
    {
        return ContainsPrototypePollutionInElement(jsonDoc.RootElement);
    }

    private static bool ContainsPrototypePollutionInElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var enumerator = element.EnumerateObject();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Name.Equals("__proto__", StringComparison.Ordinal) ||
                    enumerator.Current.Name.Equals("constructor", StringComparison.Ordinal))
                {
                    return true;
                }

                if (ContainsPrototypePollutionInElement(enumerator.Current.Value))
                    return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var enumerator = element.EnumerateArray();
            while (enumerator.MoveNext())
            {
                if (ContainsPrototypePollutionInElement(enumerator.Current))
                    return true;
            }
        }
        return false;
    }

    private static bool ContainsCircularReferences(JsonDocument jsonDoc, int maxDepth)
    {
        return CalculateDepth(jsonDoc.RootElement) > maxDepth;
    }

    private static int CalculateDepth(JsonElement element, int currentDepth = 0)
    {
        if (currentDepth > 100) return 101;

        var maxChildDepth = currentDepth;

        if (element.ValueKind == JsonValueKind.Object)
        {
            var enumerator = element.EnumerateObject();
            while (enumerator.MoveNext())
            {
                var childDepth = CalculateDepth(enumerator.Current.Value, currentDepth + 1);
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var enumerator = element.EnumerateArray();
            while (enumerator.MoveNext())
            {
                var childDepth = CalculateDepth(enumerator.Current, currentDepth + 1);
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }
        }

        return maxChildDepth;
    }

    private static bool ExceedsSizeLimit(string json, int maxSizeBytes)
    {
        return Encoding.UTF8.GetByteCount(json) > maxSizeBytes;
    }

    private static bool ContainsDisallowedPatterns(JsonDocument jsonDoc, HashSet<string> disallowedPatterns)
    {
        var jsonString = jsonDoc.RootElement.GetRawText();
        foreach (var pattern in disallowedPatterns)
        {
            if (jsonString.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static List<string> CheckRequiredFields(JsonDocument jsonDoc, HashSet<string> requiredFields)
    {
        var missing = new List<string>();
        var enumerator = jsonDoc.RootElement.EnumerateObject();

        foreach (var required in requiredFields)
        {
            var found = false;
            enumerator.Reset();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Name.Equals(required, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                missing.Add(required);
        }

        return missing;
    }
}
