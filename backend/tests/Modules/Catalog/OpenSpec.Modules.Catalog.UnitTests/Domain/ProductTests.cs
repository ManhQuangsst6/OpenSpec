using OpenSpec.Modules.Catalog.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Catalog.UnitTests.Domain;

public class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ProductData Valid(string sku = "AO-THUN-01") =>
        new(sku, "Áo thun", "Cotton 100%", 150_000m, 10, true);

    private static IReadOnlyDictionary<string, string[]> FieldErrors(Result result) =>
        Assert.IsType<ValidationError>(result.Error).Errors;

    [Fact]
    public void Create_WithValidData_NormalizesSkuAndSetsTimestamps()
    {
        var result = Product.Create(Valid(" ao-thun-01 ") with { Name = "  Áo thun ", Description = "   " }, Now);

        Assert.True(result.IsSuccess);
        var product = result.Value!;
        Assert.Equal("AO-THUN-01", product.Sku);
        Assert.Equal("Áo thun", product.Name);
        Assert.Null(product.Description);
        Assert.Equal((Now, Now), (product.CreatedAt, product.UpdatedAt));
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("SKU WITH SPACE")]
    [InlineData("SKU_01")]
    [InlineData("ÁO-01")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456")] // 33 ký tự
    public void Create_WithInvalidSku_FailsOnSku(string sku)
    {
        var result = Product.Create(Valid(sku), Now);

        Assert.Equal("Product.Validation", result.Error!.Code);
        Assert.Equal(["sku"], FieldErrors(result).Keys);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_000_000.01)]
    [InlineData(10.005)]
    public void Create_WithInvalidPrice_FailsOnPrice(double price)
    {
        var result = Product.Create(Valid() with { Price = (decimal)price }, Now);

        Assert.Equal(["price"], FieldErrors(result).Keys);
    }

    [Fact]
    public void Create_WithBoundaryValues_Succeeds()
    {
        var data = Valid("ABC") with { Name = new string('a', 200), Description = new string('d', 2000), Price = 1_000_000_000m, Stock = 0 };

        Assert.True(Product.Create(data, Now).IsSuccess);
    }

    [Fact]
    public void Create_WithManyInvalidFields_ReportsAllAtOnce()
    {
        var data = new ProductData("x", " ", new string('d', 2001), -5m, -1, true);

        var result = Product.Create(data, Now);

        Assert.Equal(["sku", "name", "description", "price", "stock"], FieldErrors(result).Keys);
    }

    [Fact]
    public void Update_ChangesFieldsAndUpdatedAtButKeepsCreatedAt()
    {
        var product = Product.Create(Valid(), Now).Value!;
        var later = Now.AddHours(1);

        var result = product.Update(Valid("NEW-01") with { Price = 99_000m, IsActive = false }, later);

        Assert.True(result.IsSuccess);
        Assert.Equal(("NEW-01", 99_000m, false), (product.Sku, product.Price, product.IsActive));
        Assert.Equal((Now, later), (product.CreatedAt, product.UpdatedAt));
    }

    [Fact]
    public void Update_WithInvalidData_LeavesProductUnchanged()
    {
        var product = Product.Create(Valid(), Now).Value!;

        var result = product.Update(Valid() with { Name = "Mới", Stock = -1 }, Now.AddHours(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(("Áo thun", 10, Now), (product.Name, product.Stock, product.UpdatedAt));
    }
}
