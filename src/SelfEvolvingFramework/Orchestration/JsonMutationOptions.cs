namespace SelfEvolvingFramework.Orchestration;

public sealed class JsonMutationOptions
{
    public string Objective { get; set; } = 
        "Improve the JSON structure, add missing fields, fix issues, and optimize the design.";
    
    public string SystemPrompt { get; set; } = 
        "You are a JSON evolution engine. Return only valid JSON with no markdown, explanations, or extra text.";
    
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4000;
    public double TopP { get; set; } = 0.9;
}
