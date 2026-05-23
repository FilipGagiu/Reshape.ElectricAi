using Reshape.ElectricAi.Core.Dtos.VectorSearch;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Tests.Fakes;

public sealed class FakeVectorSearchService : IVectorSearchService
{
    public List<DocumentSearchFilter> DocumentCalls { get; } = [];
    public List<EventSearchFilter> EventCalls { get; } = [];

    public IReadOnlyList<RetrievedChunk> DocumentResults { get; set; } = [];
    public IReadOnlyList<RetrievedEvent> EventResults { get; set; } = [];

    public Task<IReadOnlyList<RetrievedChunk>> SearchDocumentsAsync(DocumentSearchFilter filter, CancellationToken cancellationToken = default)
    {
        DocumentCalls.Add(filter);
        return Task.FromResult(DocumentResults);
    }

    public Task<IReadOnlyList<RetrievedQA>> SearchQuestionsAsync(QuestionSearchFilter filter, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RetrievedQA>>([]);

    public Task<IReadOnlyList<RetrievedEvent>> SearchEventsAsync(EventSearchFilter filter, CancellationToken cancellationToken = default)
    {
        EventCalls.Add(filter);
        return Task.FromResult(EventResults);
    }
}
