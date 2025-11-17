using System;
using System.Collections.Generic;
using Spectre.Console;

namespace SelfPlusPlusCLI.Common;

public class DisplayContext
{
    public DisplayContext() { }

    public string FormatDurationMinutes(double? minutes)
    {
        if (!minutes.HasValue)
        {
            return string.Empty;
        }

        var totalMinutes = Math.Round(minutes.Value, 2, MidpointRounding.AwayFromZero);

        if (totalMinutes >= 60)
        {
            var hours = (int)(totalMinutes / 60);
            var remainingMinutes = (int)(totalMinutes % 60);
            if (remainingMinutes > 0)
            {
                return $"{hours}h {remainingMinutes}m";
            }
            else
            {
                return $"{hours}h";
            }
        }
        else
        {
            return $"{totalMinutes}m";
        }
    }

    // Method overloads for BuildLabeledValue
    public string BuildLabeledValue(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"{Markup.Escape(label)}: [bold]{Markup.Escape(value)}[/]";
    }

    public string BuildLabeledValue(string label, double? value, string? unit = null)
    {
        if (!value.HasValue && string.IsNullOrWhiteSpace(unit))
        {
            return string.Empty;
        }

        string formatted;
        if (value.HasValue)
        {
            formatted = value.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(unit))
            {
                formatted += $"{unit}";
            }
        }
        else
        {
            // Only unit, no value
            formatted = unit!;
        }

        return $"{Markup.Escape(label)}: [bold]{Markup.Escape(formatted)}[/]";
    }


    public void AddIfNotNull(List<string> segments, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            segments.Add(value);
        }
    }
}
