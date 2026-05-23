namespace Reshape.ElectricAi.Core.Persistence;

public interface IRepository<T>
    where T : class
{
    ValueTask<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
