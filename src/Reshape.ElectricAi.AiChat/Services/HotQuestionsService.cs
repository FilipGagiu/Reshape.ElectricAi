using Reshape.ElectricAi.Core.Dtos.Conversation;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.AiChat.Services;

/// <summary>
/// Mocked hot-questions provider. Returns a static, festival-logistics-flavoured list
/// in descending count order. Future work: replace with an embedding-cluster query
/// against <c>chat.conversation_messages</c> filtered to <c>Actor = User</c> and the
/// last 24h, surfacing the top-N cluster representatives. Wire contract stays identical.
/// </summary>
internal sealed class HotQuestionsService : IHotQuestionsService
{
    private static readonly IReadOnlyList<HotQuestionDto> Mocked =
    [
        new(
            "What time does the main stage open on opening night?",
            73,
            "Main stage gates open at 17:00 on opening night. First act hits the stage at 18:30."),
        new(
            "Where's the closest camping to the main stage?",
            58,
            "Greencamp is the closest, roughly a 10-minute walk from the main stage entrance."),
        new(
            "Are there shuttles from Cluj-Napoca city centre?",
            41,
            "Yes. Shuttles run every 30 minutes from Piata Mihai Viteazu, 14:00 to 02:00 daily."),
        new(
            "What food options exist for vegans?",
            29,
            "Green Kitchen zone near the main stage has 12 fully vegan and gluten-free vendors, open all festival hours."),
        new(
            "What's the weather forecast for the festival?",
            22,
            "Mostly sunny, highs around 28C, with a chance of light evening showers on day 3. Bring a light layer for nights.")
    ];

    public Task<IReadOnlyList<HotQuestionDto>> GetTopAsync(int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var take = count <= 0 ? 0 : Math.Min(count, Mocked.Count);
        IReadOnlyList<HotQuestionDto> result = take == Mocked.Count
            ? Mocked
            : Mocked.Take(take).ToList();

        return Task.FromResult(result);
    }
}
