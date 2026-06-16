namespace SelfEvolvingFramework.Orchestration;

internal static class ExecutionBudgetContext
{
    private static readonly AsyncLocal<int?> CurrentBudgetMilliseconds = new();

    public static int? CurrentExecutionBudgetMilliseconds => CurrentBudgetMilliseconds.Value;

    public static IDisposable BeginScope(int executionBudgetMilliseconds)
    {
        var previous = CurrentBudgetMilliseconds.Value;
        CurrentBudgetMilliseconds.Value = executionBudgetMilliseconds;
        return new Scope(previous);
    }

    private sealed class Scope(int? previous) : IDisposable
    {
        public void Dispose()
        {
            CurrentBudgetMilliseconds.Value = previous;
        }
    }
}
