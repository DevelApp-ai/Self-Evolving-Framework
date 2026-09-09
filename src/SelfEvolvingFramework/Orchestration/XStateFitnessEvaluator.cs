using System.Text.Json;
using SelfEvolvingFramework.Core;

namespace SelfEvolvingFramework.Orchestration;

public sealed class XStateFitnessEvaluator : IFitnessEvaluator
{
    private readonly XStateFitnessOptions _options;

    public XStateFitnessEvaluator(XStateFitnessOptions? options = null)
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
            using var jsonDoc = LenientJson.Parse(candidate.SourceMaterial);

            if (HasRequiredFields(jsonDoc))
                score += _options.RequiredFieldsWeight;

            if (HasValidInitialState(jsonDoc))
                score += _options.InitialStateWeight;

            var reachability = CalculateStateReachability(jsonDoc);
            score += reachability * _options.ReachabilityWeight;

            var transitionScore = EvaluateTransitions(jsonDoc);
            score += transitionScore * _options.TransitionsWeight;

            if (HasErrorHandling(jsonDoc))
                score += _options.ErrorHandlingWeight;

            var complexity = CalculateComplexity(jsonDoc);
            score -= complexity * _options.ComplexityPenalty;

            if (HasDocumentation(jsonDoc))
                score += _options.DocumentationBonus;

            return Math.Max(0, score);
        }
        catch (JsonException)
        {
            return double.NegativeInfinity;
        }
    }

    private static bool HasRequiredFields(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        return root.TryGetProperty("id", out _) &&
               root.TryGetProperty("initial", out _) &&
               root.TryGetProperty("states", out _);
    }

    private static bool HasValidInitialState(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        if (root.TryGetProperty("states", out var states) &&
            root.TryGetProperty("initial", out var initial))
        {
            var initialState = initial.GetString();
            if (states.ValueKind == JsonValueKind.Object)
            {
                var enumerator = states.EnumerateObject();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Name.Equals(initialState, StringComparison.Ordinal))
                        return true;
                }
            }
        }
        return false;
    }

    private static double CalculateStateReachability(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        if (!root.TryGetProperty("states", out var states) ||
            states.ValueKind != JsonValueKind.Object)
            return 0.5;

        var totalStates = 0;
        var reachableStates = new HashSet<string>();

        var enumerator = states.EnumerateObject();
        while (enumerator.MoveNext())
        {
            totalStates++;
            var stateName = enumerator.Current.Name;
            var state = enumerator.Current.Value;

            if (IsStateReachable(state, root))
                reachableStates.Add(stateName);
        }

        return totalStates > 0 ? (double)reachableStates.Count / totalStates : 0.5;
    }

    private static bool IsStateReachable(JsonElement state, JsonElement root)
    {
        if (!root.TryGetProperty("initial", out var initial))
            return false;

        var initialState = initial.GetString();
        if (state.ValueKind == JsonValueKind.Object &&
            state.TryGetProperty("id", out var stateId) &&
            stateId.GetString() == initialState)
            return true;

        if (state.TryGetProperty("on", out var transitions) &&
            transitions.ValueKind == JsonValueKind.Object)
        {
            var transitionEnumerator = transitions.EnumerateObject();
            while (transitionEnumerator.MoveNext())
            {
                var target = transitionEnumerator.Current.Value;
                if (target.ValueKind == JsonValueKind.String)
                {
                    var targetState = target.GetString();
                    if (root.TryGetProperty("states", out var states) &&
                        states.ValueKind == JsonValueKind.Object)
                    {
                        var statesEnumerator = states.EnumerateObject();
                        while (statesEnumerator.MoveNext())
                        {
                            if (statesEnumerator.Current.Name.Equals(targetState, StringComparison.Ordinal))
                                return IsStateReachable(statesEnumerator.Current.Value, root);
                        }
                    }
                }
            }
        }

        return false;
    }

    private static double EvaluateTransitions(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        if (!root.TryGetProperty("states", out var states) ||
            states.ValueKind != JsonValueKind.Object)
            return 0.5;

        var totalTransitions = 0;
        var validTransitions = 0;

        var enumerator = states.EnumerateObject();
        while (enumerator.MoveNext())
        {
            var state = enumerator.Current.Value;
            if (state.TryGetProperty("on", out var transitions) &&
                transitions.ValueKind == JsonValueKind.Object)
            {
                var transitionEnumerator = transitions.EnumerateObject();
                while (transitionEnumerator.MoveNext())
                {
                    totalTransitions++;
                    var target = transitionEnumerator.Current.Value;
                    if (target.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(target.GetString()))
                    {
                        validTransitions++;
                    }
                }
            }
        }

        return totalTransitions > 0 ? (double)validTransitions / totalTransitions : 0.5;
    }

    private static bool HasErrorHandling(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        if (root.TryGetProperty("states", out var states) &&
            states.ValueKind == JsonValueKind.Object)
        {
            var enumerator = states.EnumerateObject();
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current.Value;
                if (state.TryGetProperty("onError", out _))
                    return true;
            }
        }
        return false;
    }

    private static double CalculateComplexity(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        var complexity = 0.0;

        if (root.TryGetProperty("states", out var states) &&
            states.ValueKind == JsonValueKind.Object)
        {
            complexity += states.EnumerateObject().Count();

            var enumerator = states.EnumerateObject();
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current.Value;
                if (state.TryGetProperty("on", out var transitions) &&
                    transitions.ValueKind == JsonValueKind.Object)
                {
                    complexity += transitions.EnumerateObject().Count();
                }
            }
        }

        return Math.Min(complexity / 100.0, 1.0);
    }

    private static bool HasDocumentation(JsonDocument jsonDoc)
    {
        var root = jsonDoc.RootElement;
        return root.TryGetProperty("description", out _) ||
               root.TryGetProperty("meta", out _);
    }
}
