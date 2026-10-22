using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenSpec.Modules.Catalog.Infrastructure.Persistence;

/// <summary>Cho phép `dotnet ef` tạo/áp dụng migration mà không cần chạy Host.</summary>
internal sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string LocalConnection =
        @"Server=.\SQLEXPRESS;Database=OpenSpec;Trusted_Connection=True;TrustServerCertificate=True";

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? LocalConnection;

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema))
            .Options;

        return new CatalogDbContext(options);
    }
}
