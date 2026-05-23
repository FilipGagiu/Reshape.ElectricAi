using Reshape.ElectricAi.Core.Dtos.VectorSearch;

namespace Reshape.ElectricAi.Core.Services;

public interface IIngestService
{
    Task IngestDocumentAsync(IngestDocumentRequest request, CancellationToken cancellationToken = default);
    Task IngestQAAsync(IngestQARequest request, CancellationToken cancellationToken = default);
    Task IngestEventAsync(IngestEventRequest request, CancellationToken cancellationToken = default);
    Task RemoveEventAsync(Guid feedEntryId, CancellationToken cancellationToken = default);
}
