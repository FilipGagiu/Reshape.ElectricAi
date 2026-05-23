using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.Core.Services;

public interface IVectorSearchService
{
    Task<IReadOnlyList<RetrievedChunk>> SearchDocumentsAsync(DocumentSearchFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetrievedQA>> SearchQuestionsAsync(QuestionSearchFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RetrievedEvent>> SearchEventsAsync(EventSearchFilter filter, CancellationToken cancellationToken = default);
}
