using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Specifications;

public sealed class ArtistChunksMissingTagsSpec : Specification<DocumentChunk>
{
    public ArtistChunksMissingTagsSpec()
    {
        Where(c => c.Content.StartsWith("Artist:") && c.CategoryTags.Length == 0);
    }
}
