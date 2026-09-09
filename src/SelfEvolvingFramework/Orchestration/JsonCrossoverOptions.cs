namespace SelfEvolvingFramework.Orchestration;

public sealed class JsonCrossoverOptions
{
    public string Objective { get; set; } = 
        "Combine the best traits from both JSON structures to create an improved version.";
    
    public string SystemPrompt { get; set; } = 
        "You are a JSON crossover engine. Return only valid JSON with no markdown, explanations, or extra text.";
}
