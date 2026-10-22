using Microsoft.EntityFrameworkCore;
using OpenSpec.Modules.Catalog.Domain.Entities;
using OpenSpec.Shared.Infrastructure.Persistence;

namespace OpenSpec.Modules.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : ModuleDbContext(options, Schema)
{
    public const string Schema = "catalog";

    public DbSet<Product> Products => Set<Product>();
}
