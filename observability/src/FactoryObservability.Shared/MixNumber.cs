using System.Text.RegularExpressions;

namespace FactoryObservability.Shared;

public static partial class MixNumber
{
    private static readonly Regex Digits7 = MyRegex();

    /// <summary>Validates and normalizes a seven-digit mix number string.</summary>
    public static bool TryNormalize(string? raw, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "mixNumber is required";
            return false;
        }

        var s = raw.Trim();
        if (!Digits7.IsMatch(s))
        {
            error = "mixNumber must be exactly 7 digits";
            return false;
        }

        normalized = s;
        return true;
    }

    [GeneratedRegex(@"^\d{7}$", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
