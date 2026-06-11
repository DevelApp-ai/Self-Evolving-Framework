using System.Reflection;
using System.Runtime.CompilerServices;

namespace SelfEvolvingFramework.Execution;

public sealed class IsolatedAssemblyExecutor : IIsolatedAssemblyExecutor
{
    public async Task<ExecutionResult> ExecuteStaticAsync(byte[] assemblyBytes, string typeName, string methodName, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (timeout <= TimeSpan.Zero)
        {
            return ExecutionResult.Failed(["Timeout must be greater than zero."]);
        }

        WeakReference? weakReference = null;

        try
        {
            var executionTask = Task.Run(() => ExecuteAndUnload(assemblyBytes, typeName, methodName, out weakReference), cancellationToken);
            var returnValue = await executionTask.WaitAsync(timeout, cancellationToken);
            ForceCollectUntilUnloaded(weakReference!);
            return new ExecutionResult(true, returnValue, []);
        }
        catch (TimeoutException)
        {
            if (weakReference is not null)
            {
                ForceCollectUntilUnloaded(weakReference);
            }

            return ExecutionResult.Failed([$"Execution exceeded timeout: {timeout}."]);
        }
        catch (OperationCanceledException)
        {
            return ExecutionResult.Failed(["Execution canceled."]);
        }
        catch (Exception ex)
        {
            if (weakReference is not null)
            {
                ForceCollectUntilUnloaded(weakReference);
            }

            return ExecutionResult.Failed([ex.Message]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object? ExecuteAndUnload(byte[] assemblyBytes, string typeName, string methodName, out WeakReference weakReference)
    {
        var context = new IsolatedAssemblyLoadContext();
        weakReference = new WeakReference(context, trackResurrection: false);

        using var stream = new MemoryStream(assemblyBytes);
        var assembly = context.LoadFromStream(stream);
        var type = assembly.GetType(typeName, throwOnError: true)
                   ?? throw new TypeLoadException($"Type '{typeName}' was not found.");
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                     ?? throw new MissingMethodException(typeName, methodName);
        if (method.GetParameters().Length > 0)
        {
            throw new InvalidOperationException($"Method '{typeName}.{methodName}' must not declare parameters.");
        }

        var invocationResult = method.Invoke(null, null);
        var result = ResolveInvocationResult(invocationResult);
        context.Unload();
        return result;
    }

    private static object? ResolveInvocationResult(object? invocationResult)
    {
        if (invocationResult is not Task task)
        {
            return invocationResult;
        }

        task.GetAwaiter().GetResult();
        var taskType = task.GetType();
        return taskType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(task);
    }

    private static void ForceCollectUntilUnloaded(WeakReference weakReference)
    {
        for (var i = 0; i < 10 && weakReference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
