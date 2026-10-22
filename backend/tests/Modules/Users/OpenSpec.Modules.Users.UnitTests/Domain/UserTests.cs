using OpenSpec.Modules.Users.Domain.Entities;

namespace OpenSpec.Modules.Users.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Create_WithValidEmail_NormalizesEmail()
    {
        var result = User.Create("  Alice@Example.COM ");

        Assert.True(result.IsSuccess);
        Assert.Equal("alice@example.com", result.Value!.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Create_WithInvalidEmail_Fails(string email)
    {
        var result = User.Create(email);

        Assert.False(result.IsSuccess);
        Assert.Equal("User.InvalidEmail", result.Error!.Code);
    }
}
