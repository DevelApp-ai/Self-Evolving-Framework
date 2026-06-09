using System.Reflection;
using System.Runtime.CompilerServices;

namespace SelfEvolving.Framework.Execution;

public sealed class IsolatedAssemblyExecutor : IIsolatedAssemblyExecutor
{
    public async Task<ExecutionResult> ExecuteStaticAsync(byte[] assemblyBytes, string typeName, string methodName, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
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

        var result = method.Invoke(null, null);
        context.Unload();
        return result;
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
