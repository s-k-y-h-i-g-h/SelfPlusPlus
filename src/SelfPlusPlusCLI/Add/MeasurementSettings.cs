using Spectre.Console;
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

    [CommandArgument(3, "[UNIT]")]
    public string? Unit { get; set; }

    public override ValidationResult Validate()
    {
        if (Category == MeasurementCategory.Subjective)
        {
            if (Value < 0 || Value > 10)
            {
                return ValidationResult.Error("Value must be between 0 and 10 for subjective measurements.");
            }

            var rounded = MathF.Round(Value);
            if (MathF.Abs(Value - rounded) > float.Epsilon)
            {
                return ValidationResult.Error("Value must be an integer for subjective measurements.");
            }
        }
        else if (string.IsNullOrWhiteSpace(Unit))
        {
            return ValidationResult.Error($"Unit is required for measurement category '{Category}'.");
        }

        return ValidationResult.Success();
    }
}
