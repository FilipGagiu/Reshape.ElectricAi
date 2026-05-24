using System.Reflection;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services;

public sealed class ConversationTitleHelperTests
{
    private static string Derive(string input, int max) =>
        (string)InvokeHelper(input, max)!;

    private static object? InvokeHelper(string input, int max)
    {
        var asm = Assembly.Load("Reshape.ElectricAi.AiChat");
        var type = asm.GetType("Reshape.ElectricAi.AiChat.Services.ConversationTitleHelper", throwOnError: true)!;
        var method = type.GetMethod("Derive", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        return method.Invoke(null, [input, max]);
    }

    [Fact]
    public void Derive_ShortMessage_ReturnsTrimmed()
    {
        Derive("  Hello there  ", 60).Should().Be("Hello there");
    }

    [Fact]
    public void Derive_LongMessage_TruncatesAtWordBoundary()
    {
        var input = "What time does the main stage open and how do I get to the festival?";
        var title = Derive(input, 30);
        title.Length.Should().BeLessThanOrEqualTo(31);
        title.Should().EndWith("…");
        title.Should().NotContain("  ");
    }

    [Fact]
    public void Derive_CollapsesWhitespace()
    {
        Derive("a\n\nb   c", 60).Should().Be("a b c");
    }

    [Fact]
    public void Derive_EmptyOrWhitespace_ReturnsUntitled()
    {
        Derive("", 60).Should().Be("Untitled");
        Derive("   ", 60).Should().Be("Untitled");
    }

    [Fact]
    public void Derive_NoWordBoundaryInFirstHalf_FallsBackToHardSlice()
    {
        var input = string.Concat(new string('a', 100), " tail");
        var title = Derive(input, 20);
        title.Length.Should().Be(21);
        title.Should().EndWith("…");
    }
}
