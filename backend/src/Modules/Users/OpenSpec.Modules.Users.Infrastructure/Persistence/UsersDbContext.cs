using Microsoft.EntityFrameworkCore;
using OpenSpec.Modules.Users.Domain.Entities;
using OpenSpec.Shared.Infrastructure.Persistence;

namespace OpenSpec.Modules.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : ModuleDbContext(options, Schema)
{
    public const string Schema = "users";

    public DbSet<User> Users => Set<User>();
}
