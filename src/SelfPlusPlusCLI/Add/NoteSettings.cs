using Spectre.Console;
using Spectre.Console.Cli;

namespace SelfPlusPlusCLI.Add;

public class NoteSettings : AddSettings
{
    [CommandArgument(1, "<CONTENT>")]
    public string Content { get; set; } = string.Empty;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            return ValidationResult.Error("Content must not be empty.");
        }

        return ValidationResult.Success();
    }
}
