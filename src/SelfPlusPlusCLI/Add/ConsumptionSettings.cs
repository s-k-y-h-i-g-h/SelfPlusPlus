using Spectre.Console;
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

    [CommandArgument(2, "[AMOUNT]")]
    public float? Amount { get; set; }

    [CommandArgument(3, "[UNIT]")]
    public string? Unit { get; set; }

    public override ValidationResult Validate()
    {
        if (Category != ConsumptionCategory.Stack)
        {
            if (!Amount.HasValue)
            {
                return ValidationResult.Error($"Amount is required for consumption category '{Category}'.");
            }

            if (string.IsNullOrWhiteSpace(Unit))
            {
                return ValidationResult.Error($"Unit is required for consumption category '{Category}'.");
            }
        }
        else
        {
            var amountProvided = Amount.HasValue;
            var unitProvided = !string.IsNullOrWhiteSpace(Unit);

            if (amountProvided != unitProvided)
            {
                return ValidationResult.Error("For stack consumptions, amount and unit must be provided together or omitted together.");
            }
        }

        return ValidationResult.Success();
    }
}
