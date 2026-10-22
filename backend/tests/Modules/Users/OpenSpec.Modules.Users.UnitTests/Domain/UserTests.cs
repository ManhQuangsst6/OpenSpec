using OpenSpec.Modules.Users.Domain.Entities;

namespace OpenSpec.Modules.Users.UnitTests.Domain;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_NormalizesEmailAndTrimsName()
    {
        var result = User.Create("  Alice@Example.COM ", "  Alice Nguyễn ", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("alice@example.com", result.Value!.Email);
        Assert.Equal("Alice Nguyễn", result.Value.FullName);
        Assert.Equal(Now, result.Value.CreatedAt);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b@c.com")]
    [InlineData("Alice <alice@example.com>")]
    public void Create_WithInvalidEmail_Fails(string email)
    {
        var result = User.Create(email, "Alice", Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("User.InvalidEmail", result.Error!.Code);
    }

    [Fact]
    public void Create_WithTooLongEmail_Fails()
    {
        var email = new string('a', 251) + "@x.com"; // 257 ký tự

        Assert.Equal("User.InvalidEmail", User.Create(email, "Alice", Now).Error!.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_Fails(string fullName)
    {
        var result = User.Create("alice@example.com", fullName, Now);

        Assert.Equal("User.InvalidFullName", result.Error!.Code);
    }

    [Fact]
    public void Create_WithTooLongName_Fails()
    {
        var result = User.Create("alice@example.com", new string('a', 201), Now);

        Assert.Equal("User.InvalidFullName", result.Error!.Code);
    }

    [Fact]
    public void Update_WithValidData_ChangesFieldsButKeepsIdentity()
    {
        var user = User.Create("alice@example.com", "Alice", Now).Value!;
        var id = user.Id;

        var result = user.Update("BOB@example.com", "Bob");

        Assert.True(result.IsSuccess);
        Assert.Equal("bob@example.com", user.Email);
        Assert.Equal("Bob", user.FullName);
        Assert.Equal(id, user.Id);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Fact]
    public void Update_WithInvalidData_LeavesUserUnchanged()
    {
        var user = User.Create("alice@example.com", "Alice", Now).Value!;

        var result = user.Update("bad", "Bob");

        Assert.False(result.IsSuccess);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("Alice", user.FullName);
    }
}
