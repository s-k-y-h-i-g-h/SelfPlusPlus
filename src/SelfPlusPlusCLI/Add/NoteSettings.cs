using Spectre.Console;
using Spectre.Console.Cli;

namespace SelfPlusPlusCLI.Add;

public class NoteSettings : AddSettings
{
    [CommandArgument(0, "<CATEGORY>")]
    public string Category { get; set; } = string.Empty;

    [CommandArgument(1, "<CONTENT>")]
    public string Content { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Category))
        {
            return ValidationResult.Error("Category must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(Content))
        {
            return ValidationResult.Error("Content must not be empty.");
        }

        return ValidationResult.Success();
    }
}
