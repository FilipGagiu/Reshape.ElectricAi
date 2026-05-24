namespace Reshape.ElectricAi.Core.Services;

public interface ITopArtistsService
{
    Task<IReadOnlyList<string>> GetTopForUserAsync(Guid userId, CancellationToken cancellationToken);
}
