using Spectre.Console.Cli;

namespace SelfPlusPlusCLI.Add;

public enum MeasurementCategory
{
    Vitals,
    Subjective
}

public class MeasurementSettings : AddSettings
{
    [CommandArgument(0, "<CATEGORY>")]
    public MeasurementCategory Category { get; set; }

    [CommandArgument(1, "<NAME>")]
    public string Name { get; set; } = string.Empty;

    [CommandArgument(2, "<VALUE>")]
    public float Value { get; set; }

    [CommandArgument(3, "<UNIT>")]
    public string Unit { get; set; } = string.Empty;
}
