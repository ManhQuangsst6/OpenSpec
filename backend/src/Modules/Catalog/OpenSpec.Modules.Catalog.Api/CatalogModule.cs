using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenSpec.Modules.Catalog.Application.Products;
using OpenSpec.Modules.Catalog.Domain.Entities;
using OpenSpec.Modules.Catalog.Infrastructure;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Catalog.Api;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCatalogInfrastructure(configuration.GetConnectionString("Default"));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ProductService>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");

        group.MapGet("/ping", () => TypedResults.Ok("catalog"))
            .WithName("CatalogPing");

        var products = group.MapGroup("/products");
        products.MapGet("/", ListProducts).WithName("ListProducts");
        products.MapGet("/{id:guid}", GetProduct).WithName("GetProduct")
            .ProducesProblem(StatusCodes.Status404NotFound);
        products.MapPost("/", CreateProduct).WithName("CreateProduct")
            .ProducesProblem(StatusCodes.Status409Conflict);
        products.MapPut("/{id:guid}", UpdateProduct).WithName("UpdateProduct")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        products.MapDelete("/{id:guid}", DeleteProduct).WithName("DeleteProduct")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<PagedResult<ProductDto>>, ValidationProblem>> ListProducts(
        ProductService service,
        CancellationToken ct,
        int page = 1,
        int pageSize = ProductService.DefaultPageSize,
        string? search = null,
        bool? isActive = null,
        [Description("Cột sắp xếp: sku, name, price, stock, createdAt (mặc định createdAt giảm dần).")] string? sortBy = null,
        [Description("Chiều sắp xếp: asc hoặc desc.")] string? sortDir = null)
    {
        var query = ProductService.ParseQuery(page, pageSize, search, isActive, sortBy, sortDir);
        if (!query.IsSuccess)
            return Validation((ValidationError)query.Error!);

        var result = await service.ListAsync(query.Value!, ct);
        return TypedResults.Ok(result.Value!);
    }

    private static async Task<Results<Ok<ProductDto>, ProblemHttpResult>> GetProduct(Guid id, ProductService service, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return result.IsSuccess ? TypedResults.Ok(result.Value!) : Problem(result.Error!);
    }

    private static async Task<Results<Created<ProductDto>, ValidationProblem, ProblemHttpResult>> CreateProduct(
        SaveProductRequest request, ProductService service, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        if (result.IsSuccess)
            return TypedResults.Created($"/api/catalog/products/{result.Value!.Id}", result.Value);

        return result.Error is ValidationError invalid ? Validation(invalid) : Problem(result.Error!);
    }

    private static async Task<Results<Ok<ProductDto>, ValidationProblem, ProblemHttpResult>> UpdateProduct(
        Guid id, SaveProductRequest request, ProductService service, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (result.IsSuccess)
            return TypedResults.Ok(result.Value!);

        return result.Error is ValidationError invalid ? Validation(invalid) : Problem(result.Error!);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteProduct(Guid id, ProductService service, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.IsSuccess ? TypedResults.NoContent() : Problem(result.Error!);
    }

    private static ValidationProblem Validation(ValidationError error) =>
        TypedResults.ValidationProblem(error.Errors, title: error.Message, extensions: Code(error));

    private static ProblemHttpResult Problem(Error error)
    {
        var status = error == ProductErrors.NotFound ? StatusCodes.Status404NotFound
            : error == ProductErrors.SkuTaken ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

        return TypedResults.Problem(title: error.Message, statusCode: status, extensions: Code(error));
    }

    private static Dictionary<string, object?> Code(Error error) => new() { ["code"] = error.Code };
}
