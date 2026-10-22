using Microsoft.EntityFrameworkCore;

namespace OpenSpec.Shared.Infrastructure.Persistence;

/// <summary>DbContext gốc cho mỗi module; mỗi module dùng schema SQL riêng.</summary>
public abstract class ModuleDbContext(DbContextOptions options, string schema) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}
