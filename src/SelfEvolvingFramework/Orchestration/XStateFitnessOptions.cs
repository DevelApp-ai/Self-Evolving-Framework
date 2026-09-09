namespace SelfEvolvingFramework.Orchestration;

public sealed class XStateFitnessOptions
{
    public double RequiredFieldsWeight { get; set; } = 20.0;
    public double InitialStateWeight { get; set; } = 15.0;
    public double ReachabilityWeight { get; set; } = 25.0;
    public double TransitionsWeight { get; set; } = 20.0;
    public double ErrorHandlingWeight { get; set; } = 10.0;
    public double ComplexityPenalty { get; set; } = 0.1;
    public double DocumentationBonus { get; set; } = 5.0;
}
