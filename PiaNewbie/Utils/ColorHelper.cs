using System.Globalization;
using System.Text.RegularExpressions;

namespace PiaNewbie.Utils;

public static partial class ColorHelper
{
    public static bool TryNormalizeHex(string? input, out string hex)
    {
        hex = "#66CCFF";
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var s = input.Trim();
        if (!s.StartsWith('#'))
            s = "#" + s;

        if (!HexColorRegex().IsMatch(s))
            return false;

        hex = s.Length == 4
            ? $"#{s[1]}{s[1]}{s[2]}{s[2]}{s[3]}{s[3]}"
            : s.ToUpperInvariant();

        return true;
    }

    public static string AdjustBrightness(string hex, int amount)
    {
        if (!TryNormalizeHex(hex, out var normalized))
            return hex;

        var n = Convert.ToInt32(normalized[1..], 16);
        var r = Math.Clamp((n >> 16) + amount, 0, 255);
        var g = Math.Clamp(((n >> 8) & 0xff) + amount, 0, 255);
        var b = Math.Clamp((n & 0xff) + amount, 0, 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public static string WithAlpha(string hex, double alpha)
    {
        if (!TryNormalizeHex(hex, out var normalized))
            return hex;

        var n = Convert.ToInt32(normalized[1..], 16);
        var a = Math.Clamp((int)Math.Round(alpha * 255), 0, 255);
        var r = (n >> 16) & 0xff;
        var g = (n >> 8) & 0xff;
        var b = n & 0xff;
        return $"rgba({r},{g},{b},{alpha.ToString(CultureInfo.InvariantCulture)})";
    }

    [GeneratedRegex(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")]
    private static partial Regex HexColorRegex();
}
