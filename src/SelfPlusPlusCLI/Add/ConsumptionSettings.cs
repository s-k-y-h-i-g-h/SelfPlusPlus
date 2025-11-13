using Spectre.Console.Cli;

namespace SelfPlusPlusCLI.Add;

public enum ConsumptionCategory
{
    Substance,
    Food,
    Stack
}

public class ConsumptionSettings : AddSettings
{
    [CommandArgument(0, "<CATEGORY>")]
    public ConsumptionCategory Category { get; set; }

    [CommandArgument(1, "<NAME>")]
    public string Name { get; set; } = string.Empty;

    [CommandArgument(2, "<AMOUNT>")]
    public float Amount { get; set; }

    [CommandArgument(3, "<UNIT>")]
    public string Unit { get; set; } = string.Empty;
}
