using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OpenSpec.Api.IntegrationTests.Fixtures;

namespace OpenSpec.Api.IntegrationTests.Modules;

public class UsersEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record UserResponse(Guid Id, string Email, string FullName, DateTimeOffset CreatedAt);

    private sealed record PageResponse(List<UserResponse> Items, int Page, int PageSize, int TotalCount);

    // Mỗi test dùng email riêng để chạy độc lập trên cùng một DB
    private static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@example.com";

    private async Task<UserResponse> CreateAsync(string email, string fullName = "Nguyễn Văn A")
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { email, fullName });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!;
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithLocationAndNormalizedEmail()
    {
        var email = UniqueEmail("Alice");

        var response = await _client.PostAsJsonAsync("/api/users", new { email = $"  {email.ToUpperInvariant()} ", fullName = "Alice" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.Equal(email.ToLowerInvariant(), user.Email);
        Assert.Equal($"/api/users/{user.Id}", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("not-an-email", "Alice", "email")]
    [InlineData("ok@example.com", "", "fullName")]
    public async Task Create_WithInvalidData_ReturnsValidationProblem(string email, string fullName, string field)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new { email, fullName });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        var email = UniqueEmail();
        await CreateAsync(email);

        var response = await _client.PostAsJsonAsync("/api/users", new { email = email.ToUpperInvariant(), fullName = "Khác" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("User.EmailTaken", await ErrorCodeAsync(response));
    }

    [Fact]
    public async Task GetUpdateDelete_FullLifecycle()
    {
        var user = await CreateAsync(UniqueEmail());

        var fetched = await _client.GetFromJsonAsync<UserResponse>($"/api/users/{user.Id}");
        Assert.Equal(user, fetched);

        var newEmail = UniqueEmail("renamed");
        var update = await _client.PutAsJsonAsync($"/api/users/{user.Id}", new { email = newEmail, fullName = "Tên mới" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.Equal(("Tên mới", newEmail, user.Id, user.CreatedAt), (updated.FullName, updated.Email, updated.Id, updated.CreatedAt));

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/users/{user.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/users/{user.Id}")).StatusCode);

        // Email đã xoá được phép dùng lại
        await CreateAsync(newEmail);
    }

    [Fact]
    public async Task Update_ToEmailOfAnotherUser_ReturnsConflictAndKeepsData()
    {
        var a = await CreateAsync(UniqueEmail("a"));
        var b = await CreateAsync(UniqueEmail("b"), "B");

        var response = await _client.PutAsJsonAsync($"/api/users/{b.Id}", new { email = a.Email, fullName = "B2" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(b, await _client.GetFromJsonAsync<UserResponse>($"/api/users/{b.Id}"));
    }

    [Fact]
    public async Task UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/users/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PutAsJsonAsync($"/api/users/{id}", new { email = UniqueEmail(), fullName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/users/{id}")).StatusCode);
    }

    [Fact]
    public async Task List_SearchesCaseInsensitiveAndPaginates()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 25; i++)
            await CreateAsync(UniqueEmail(), $"Trần {tag.ToUpperInvariant()} {i:00}");

        var page2 = await _client.GetFromJsonAsync<PageResponse>($"/api/users?search={tag}&page=2&pageSize=20");

        Assert.Equal(25, page2!.TotalCount);
        Assert.Equal(5, page2.Items.Count);
        Assert.Equal((2, 20), (page2.Page, page2.PageSize));
        // Mới nhất trước → trang 2 chứa 5 người tạo sớm nhất
        Assert.Equal("00", page2.Items[^1].FullName[^2..]);
    }

    [Fact]
    public async Task List_SearchTreatsWildcardsLiterally()
    {
        var page = await _client.GetFromJsonAsync<PageResponse>($"/api/users?search={Uri.EscapeDataString("%_")}");

        Assert.Equal(0, page!.TotalCount);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    public async Task List_WithInvalidPaging_ReturnsBadRequest(string query)
    {
        var response = await _client.GetAsync($"/api/users?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
