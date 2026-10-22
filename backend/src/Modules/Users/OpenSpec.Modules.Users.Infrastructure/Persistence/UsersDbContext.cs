using Microsoft.EntityFrameworkCore;
using OpenSpec.Shared.Infrastructure.Persistence;

namespace OpenSpec.Modules.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : ModuleDbContext(options, Schema)
{
    public const string Schema = "users";
}
