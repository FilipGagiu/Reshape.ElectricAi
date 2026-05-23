using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.VectorDb.Entities;

namespace Reshape.ElectricAi.VectorDb.Persistence.Specifications;

public sealed class QuestionByTextHashSpec : Specification<Question>
{
    public QuestionByTextHashSpec(string textHash)
    {
        Where(q => q.TextHash == textHash);
        EnableNoTracking();
    }
}
