using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenSpec.Modules.Catalog.Infrastructure.Persistence;
using OpenSpec.Modules.Users.Infrastructure.Persistence;

namespace OpenSpec.Api.IntegrationTests.Fixtures;

/// <summary>Host API chạy trên SQL Server thật (Testcontainers), đã áp dụng migration của các module.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqlServerFixture _sql = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Default", _sql.ConnectionString);

    public async Task InitializeAsync()
    {
        await _sql.InitializeAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }
}
