namespace SelfEvolvingFramework.LlmRouting;

public interface IModelRouter
{
    IReadOnlyList<IModelEndpoint> BuildRoute(ModelInvocationContext invocationContext, IReadOnlyList<IModelEndpoint> endpoints);
}
