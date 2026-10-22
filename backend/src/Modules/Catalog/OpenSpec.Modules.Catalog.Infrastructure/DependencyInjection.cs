using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenSpec.Modules.Catalog.Application.Products;
using OpenSpec.Modules.Catalog.Infrastructure.Persistence;

namespace OpenSpec.Modules.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema)));

        services.AddScoped<IProductRepository, ProductRepository>();
        return services;
    }
}
