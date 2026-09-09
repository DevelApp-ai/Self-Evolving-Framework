using System.Text.Json;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public sealed class JsonSchemaFitnessEvaluator : IFitnessEvaluator
{
    private readonly JsonSchemaFitnessOptions _options;

    public JsonSchemaFitnessEvaluator(JsonSchemaFitnessOptions? options = null)
    {
        _options = options ?? new();
    }

    public async Task<double> EvaluateAsync(
        CandidateProgram candidate,
        CancellationToken cancellationToken = default)
    {
        if (candidate.Format != CandidateFormat.Json)
            return double.NegativeInfinity;

        try
        {
            var score = 0.0;
            using var jsonDoc = JsonDocument.Parse(candidate.SourceMaterial);

            if (HasRequiredSchemaFields(jsonDoc))
                score += _options.RequiredFieldsWeight;

            if (HasValidTypes(jsonDoc))
                score += _options.TypesWeight;

            var propertyScore = EvaluateProperties(jsonDoc);
            score += propertyScore * _options.PropertiesWeight;

            if (HasRequiredFieldsDefined(jsonDoc))
                score += _options.RequiredWeight;

            if (HasDescriptions(jsonDoc))
                score += _options.DescriptionsWeight;

            var validationScore = EvaluateValidationRules(jsonDoc);
            score += validationScore * _options.ValidationWeight;

            return Math.Max(0, score);
        }
        catch (JsonException)
        {
            return double.NegativeInfinity;
        }
    }

    private static bool HasRequiredSchemaFields(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        return root.TryGetProperty("$schema", out _) &&
               root.TryGetProperty("type", out _) &&
               root.TryGetProperty("properties", out _);
    }

    private static bool HasValidTypes(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        if (root.TryGetProperty("type", out var typeProp))
        {
            var typeValue = typeProp.GetString();
            var validTypes = new[] { "object", "array", "string", "number", "integer", "boolean", "null" };
            return validTypes.Contains(typeValue);
        }
        return false;
    }

    private static double EvaluateProperties(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        if (!root.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
            return 0.5;

        var totalProperties = 0;
        var validProperties = 0;

        var enumerator = properties.EnumerateObject();
        while (enumerator.MoveNext())
        {
            totalProperties++;
            var prop = enumerator.Current.Value;
            if (prop.ValueKind == JsonValueKind.Object)
            {
                if (prop.TryGetProperty("type", out _) ||
                    prop.TryGetProperty("$ref", out _) ||
                    prop.TryGetProperty("properties", out _))
                {
                    validProperties++;
                }
            }
        }

        return totalProperties > 0 ? (double)validProperties / totalProperties : 0.5;
    }

    private static bool HasRequiredFieldsDefined(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        return root.TryGetProperty("required", out _);
    }

    private static bool HasDescriptions(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        if (root.TryGetProperty("description", out _))
            return true;

        if (root.TryGetProperty("properties", out var properties) &&
            properties.ValueKind == JsonValueKind.Object)
        {
            var enumerator = properties.EnumerateObject();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Value.TryGetProperty("description", out _))
                    return true;
            }
        }
        return false;
    }

    private static double EvaluateValidationRules(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        var score = 0.0;
        var ruleCount = 0;

        if (root.TryGetProperty("properties", out var properties) &&
            properties.ValueKind == JsonValueKind.Object)
        {
            var enumerator = properties.EnumerateObject();
            while (enumerator.MoveNext())
            {
                var prop = enumerator.Current.Value;
                if (prop.ValueKind == JsonValueKind.Object)
                {
                    var validationRules = new[] { "minLength", "maxLength", "pattern", "minimum", "maximum", "enum", "format" };
                    foreach (var rule in validationRules)
                    {
                        if (prop.TryGetProperty(rule, out _))
                        {
                            score++;
                            ruleCount++;
                        }
                    }
                }
            }
        }

        return ruleCount > 0 ? score / ruleCount : 0.5;
    }
}
