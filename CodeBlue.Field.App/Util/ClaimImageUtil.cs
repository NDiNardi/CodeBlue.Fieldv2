namespace CodeBlue.Field.App.Util;

public static class ClaimImageUtil
{
    public const string QuickClaimPlaceholderKey = "__quick_claim_placeholder__";
    public const string QuickClaimPlaceholderUrl = "/icons/icon-192.png";

    public static bool IsQuickClaimPlaceholder(string? storageKey) =>
        string.Equals(storageKey, QuickClaimPlaceholderKey, StringComparison.Ordinal);

    public static string ResolveImageUrl(Uri? baseAddress, string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return string.Empty;
        }

        if (IsQuickClaimPlaceholder(storageKey))
        {
            return QuickClaimPlaceholderUrl;
        }

        return $"{baseAddress}files/claims/{Uri.EscapeDataString(storageKey)}";
    }
}
