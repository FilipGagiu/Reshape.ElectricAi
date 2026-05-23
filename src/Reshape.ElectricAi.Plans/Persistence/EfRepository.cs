using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Core.Persistence;

namespace Reshape.ElectricAi.Plans.Persistence;

public class EfRepository<TContext, T>(TContext context) : IRepository<T>
    where TContext : DbContext
    where T : class
{
    protected TContext Context { get; } = context;
    protected DbSet<T> Set { get; } = context.Set<T>();

    public ValueTask<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default) =>
        Set.FindAsync([id], cancellationToken);

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        await SpecificationEvaluator.Apply(Set.AsQueryable(), specification).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.ToListAsync(cancellationToken);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification).CountAsync(cancellationToken);

    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.Apply(Set.AsQueryable(), specification).AnyAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Context.SaveChangesAsync(cancellationToken);
}
