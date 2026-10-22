using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenSpec.Modules.Users.Application.Users;
using OpenSpec.Modules.Users.Infrastructure.Persistence;

namespace OpenSpec.Modules.Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<UsersDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", UsersDbContext.Schema)));

        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
