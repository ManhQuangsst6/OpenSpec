using System.Text.Json.Serialization;
using OpenSpec.Modules.Catalog.Api;
using OpenSpec.Modules.Orders.Api;
using OpenSpec.Modules.Users.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
// Số trong JSON luôn là number (mặc định web cho phép chuỗi, khiến OpenAPI sinh kiểu integer | string)
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services
    .AddUsersModule(builder.Configuration)
    .AddCatalogModule(builder.Configuration)
    .AddOrdersModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapUsersEndpoints();
app.MapCatalogEndpoints();
app.MapOrdersEndpoints();

app.Run();

public partial class Program;
