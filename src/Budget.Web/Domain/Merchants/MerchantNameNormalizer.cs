using System.Text.RegularExpressions;

namespace Budget.Web.Domain.Merchants;

public static partial class MerchantNameNormalizer
{
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex HyphenSpacing = new(@"\s*-\s*", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes a counterparty name into a stable merchant key: trims, lowercases invariantly,
    /// collapses internal whitespace runs to a single space, and normalizes spacing around hyphens.
    /// The same function is used for transaction names, merchant keys, and alias keys.
    /// </summary>
    /// <param name="name">The raw counterparty name, or <see langword="null"/>.</param>
    /// <returns>The normalized key; an empty string when the input is blank.</returns>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var normalized = name.Trim().ToLowerInvariant();
        normalized = WhitespaceRun.Replace(normalized, " ");
        normalized = HyphenSpacing.Replace(normalized, " - ");
        normalized = WhitespaceRun.Replace(normalized, " ");
        return normalized.Trim();
    }
}
