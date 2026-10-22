using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenSpec.Modules.Users.Application.Users;
using OpenSpec.Modules.Users.Domain.Entities;
using OpenSpec.Modules.Users.Infrastructure;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Users.Api;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddUsersInfrastructure(configuration.GetConnectionString("Default"));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<UserService>();

        return services;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/ping", () => TypedResults.Ok("users"))
            .WithName("UsersPing");

        group.MapGet("/", ListUsers).WithName("ListUsers");
        group.MapGet("/{id:guid}", GetUser).WithName("GetUser")
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateUser).WithName("CreateUser")
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateUser).WithName("UpdateUser")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", DeleteUser).WithName("DeleteUser")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<PagedResult<UserDto>>, ValidationProblem>> ListUsers(
        UserService service, CancellationToken ct, int page = 1, int pageSize = UserService.DefaultPageSize, string? search = null)
    {
        var result = await service.ListAsync(page, pageSize, search, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value!);

        return TypedResults.ValidationProblem(
            new Dictionary<string, string[]> { ["page"] = [result.Error!.Message], ["pageSize"] = [result.Error.Message] },
            title: result.Error.Message,
            extensions: Code(result.Error));
    }

    private static async Task<Results<Ok<UserDto>, ProblemHttpResult>> GetUser(Guid id, UserService service, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return result.IsSuccess ? TypedResults.Ok(result.Value!) : Problem(result.Error!);
    }

    private static async Task<Results<Created<UserDto>, ValidationProblem, ProblemHttpResult>> CreateUser(
        CreateUserRequest request, UserService service, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        if (result.IsSuccess)
            return TypedResults.Created($"/api/users/{result.Value!.Id}", result.Value);

        return FieldError(result.Error!) is { } invalid ? invalid : Problem(result.Error!);
    }

    private static async Task<Results<Ok<UserDto>, ValidationProblem, ProblemHttpResult>> UpdateUser(
        Guid id, UpdateUserRequest request, UserService service, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value!);

        return FieldError(result.Error!) is { } invalid ? invalid : Problem(result.Error!);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteUser(Guid id, UserService service, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.IsSuccess ? TypedResults.NoContent() : Problem(result.Error!);
    }

    /// <summary>Lỗi gắn với một trường → 400 ValidationProblem; null nếu không phải lỗi theo trường.</summary>
    private static ValidationProblem? FieldError(Error error)
    {
        string? field = error.Code switch
        {
            "User.InvalidEmail" => "email",
            "User.InvalidFullName" => "fullName",
            _ => null,
        };

        return field is null
            ? null
            : TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { [field] = [error.Message] },
                title: error.Message,
                extensions: Code(error));
    }

    private static ProblemHttpResult Problem(Error error)
    {
        var status = error == UserErrors.NotFound ? StatusCodes.Status404NotFound
            : error == UserErrors.EmailTaken ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

        return TypedResults.Problem(title: error.Message, statusCode: status, extensions: Code(error));
    }

    private static Dictionary<string, object?> Code(Error error) => new() { ["code"] = error.Code };
}
