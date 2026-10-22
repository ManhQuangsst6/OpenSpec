using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenSpec.Modules.Users.Infrastructure.Persistence;

/// <summary>Cho phép `dotnet ef` tạo/áp dụng migration mà không cần chạy Host.</summary>
internal sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    private const string LocalConnection =
        @"Server=.\SQLEXPRESS;Database=OpenSpec;Trusted_Connection=True;TrustServerCertificate=True";

    public UsersDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? LocalConnection;

        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", UsersDbContext.Schema))
            .Options;

        return new UsersDbContext(options);
    }
}
