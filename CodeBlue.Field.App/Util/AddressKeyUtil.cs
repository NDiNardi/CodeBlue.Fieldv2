using System.Text;

namespace CodeBlue.Field.App.Util;

public static class AddressKeyUtil
{
    public static string MakeAddressKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var source = value.Trim().ToUpperInvariant();
        var buffer = new StringBuilder(source.Length);

        foreach (var ch in source)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer.Append(ch);
            }
        }

        return buffer.ToString();
    }
}
