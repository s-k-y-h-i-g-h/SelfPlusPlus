using Spectre.Console.Cli;

namespace SelfPlusPlusCLI.Add;

public class MeasurementSettings : AddSettings
{
    [CommandArgument(0, "<NAME>")]
    public string Name { get; set; } = string.Empty;

    [CommandArgument(1, "<VALUE>")]
    public float Value { get; set; }

    [CommandArgument(2, "<UNIT>")]
    public string Unit { get; set; } = string.Empty;
}
