using System.Linq.Expressions;

namespace Reshape.ElectricAi.Core.Persistence;

public interface ISpecification<T>
    where T : class
{
    Expression<Func<T, bool>>? Criteria { get; }
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }
    IReadOnlyList<string> IncludeStrings { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
    bool AsNoTracking { get; }
    bool AsSplitQuery { get; }
}
