using System.Text.RegularExpressions;

namespace Partner.Api.Shared.Security;

public static partial class FilterSanitizer
{
    [GeneratedRegex("[^a-zA-Z0-9_\\-\\s@.]", RegexOptions.Compiled)]
    private static partial Regex UnsafeCharsRegex();

    public static string? NormalizeTerm(string? value, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        normalized = UnsafeCharsRegex().Replace(normalized, string.Empty);

        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        return normalized;
    }

    public static string EnsureAllowedField(string? field, IReadOnlyCollection<string> allowedFields)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException("Filter field is required.", nameof(field));
        }

        var selected = field.Trim();
        var match = allowedFields.FirstOrDefault(f => string.Equals(f, selected, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new ArgumentException("Filter field is not allowed.", nameof(field));
        }

        return match;
    }
}

