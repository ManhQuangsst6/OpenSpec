using System.Text.Json.Serialization;
using OpenSpec.Modules.Catalog.Api;
using OpenSpec.Modules.Orders.Api;
using OpenSpec.Modules.Users.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
// Số trong JSON luôn là number (mặc định web cho phép chuỗi, khiến OpenAPI sinh kiểu integer | string)
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services
    .AddUsersModule(builder.Configuration)
    .AddCatalogModule(builder.Configuration)
    .AddOrdersModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    // Body JSON hỏng / sai kiểu → 400 thay vì 500
    StatusCodeSelector = ex => ex is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError,
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.MapUsersEndpoints();
app.MapCatalogEndpoints();
app.MapOrdersEndpoints();

app.Run();

public partial class Program;
