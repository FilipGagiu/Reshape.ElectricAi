using Microsoft.EntityFrameworkCore;

namespace Reshape.ElectricAi.VectorDb.Tests;

// Test-only adapter that lets us hand a service the IDbContextFactory<T> interface while still
// controlling the underlying context's lifetime from the test (via VectorDbFixture.CreateContext).
internal sealed class TestDbContextFactory<TContext>(Func<TContext> factory) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext() => factory();

    public Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(factory());
}
