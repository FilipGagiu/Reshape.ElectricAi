using System.Text;

namespace Reshape.ElectricAi.Core.Configuration;

public static class JwtSigningKey
{
    public const int MinimumBytes = 32;

    public static byte[] Decode(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var buffer = new byte[key.Length];
        if (Convert.TryFromBase64String(key, buffer, out var written) && written >= MinimumBytes)
        {
            return buffer[..written];
        }

        return Encoding.UTF8.GetBytes(key);
    }

    public static bool LooksLikeBase64ButTooShort(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length % 4 != 0)
        {
            return false;
        }

        foreach (var ch in key)
        {
            var isBase64Char = (ch is >= 'A' and <= 'Z')
                || (ch is >= 'a' and <= 'z')
                || (ch is >= '0' and <= '9')
                || ch is '+' or '/' or '=';
            if (!isBase64Char)
            {
                return false;
            }
        }

        var buffer = new byte[key.Length];
        return Convert.TryFromBase64String(key, buffer, out var written) && written < MinimumBytes;
    }
}
