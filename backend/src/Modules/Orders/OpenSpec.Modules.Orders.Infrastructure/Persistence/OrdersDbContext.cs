using Microsoft.EntityFrameworkCore;
using OpenSpec.Shared.Infrastructure.Persistence;

namespace OpenSpec.Modules.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : ModuleDbContext(options, Schema)
{
    public const string Schema = "orders";
}
