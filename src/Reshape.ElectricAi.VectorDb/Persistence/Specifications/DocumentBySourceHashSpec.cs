using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Specifications;

public sealed class DocumentBySourceHashSpec : Specification<Document>
{
    public DocumentBySourceHashSpec(string sourceHash)
    {
        Where(d => d.SourceHash == sourceHash);
        EnableNoTracking();
    }
}
