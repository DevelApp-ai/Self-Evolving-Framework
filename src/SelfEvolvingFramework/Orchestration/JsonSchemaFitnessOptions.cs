namespace SelfEvolvingFramework.Orchestration;

public sealed class JsonSchemaFitnessOptions
{
    public double RequiredFieldsWeight { get; set; } = 15.0;
    public double TypesWeight { get; set; } = 10.0;
    public double PropertiesWeight { get; set; } = 25.0;
    public double RequiredWeight { get; set; } = 10.0;
    public double DescriptionsWeight { get; set; } = 10.0;
    public double ValidationWeight { get; set; } = 20.0;
}
