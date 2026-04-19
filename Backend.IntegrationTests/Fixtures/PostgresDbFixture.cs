using backend.data;
using backend.services.implementations;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Backend.IntegrationTests.Fixtures;

public sealed class PostgresDbFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("testing_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private bool _started;

    private async Task EnsureStartedAsync()
    {
        if (_started) return;
        await _db.StartAsync();
        _started = true;
    }

    public async Task<AppDbContext> CreateDbContextAsync()
    {
        await EnsureStartedAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_db.GetConnectionString(), o => o.UseVector())
            .EnableSensitiveDataLogging()
            .Options;

        var ctx = new AppDbContext(options);

        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var bootstrap = new BootstrapService(ctx);
        await bootstrap.Boostrap();

        return ctx;
    }

    public async ValueTask DisposeAsync()
    {
        if (_started)
            await _db.DisposeAsync();
    }
}