using System.Text.RegularExpressions;

namespace Reshape.ElectricAi.AiChat.Services;

internal static partial class ConversationTitleHelper
{
    public static string Derive(string message, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Untitled";
        }

        var collapsed = WhitespaceRun().Replace(message.Trim().ReplaceLineEndings(" "), " ");
        if (collapsed.Length <= maxChars)
        {
            return collapsed;
        }

        var window = collapsed[..maxChars];
        var lastSpace = window.LastIndexOf(' ');
        var sliceEnd = lastSpace > maxChars / 2 ? lastSpace : maxChars;
        return string.Concat(window[..sliceEnd].TrimEnd(), "…");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
