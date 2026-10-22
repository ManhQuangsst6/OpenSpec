using OpenSpec.Modules.Users.Application.Users;
using OpenSpec.Modules.Users.Domain.Entities;
using OpenSpec.Shared.Kernel.Results;

namespace OpenSpec.Modules.Users.UnitTests.Application;

public class UserServiceTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly UserService _service;

    public UserServiceTests() => _service = new UserService(_repository, TimeProvider.System);

    [Fact]
    public async Task Create_WithNewEmail_AddsUser()
    {
        var result = await _service.CreateAsync(new CreateUserRequest("Alice@Example.com", "Alice"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("alice@example.com", result.Value!.Email);
        Assert.Single(_repository.Users);
    }

    [Fact]
    public async Task Create_WithExistingEmail_ReturnsEmailTaken()
    {
        await _service.CreateAsync(new CreateUserRequest("alice@example.com", "Alice"), default);

        var result = await _service.CreateAsync(new CreateUserRequest("ALICE@example.com", "Other"), default);

        Assert.Equal("User.EmailTaken", result.Error!.Code);
        Assert.Single(_repository.Users);
    }

    [Fact]
    public async Task Create_WhenDatabaseRejectsDuplicate_ReturnsEmailTaken()
    {
        _repository.FailNextSaveWith = UserErrors.EmailTaken;

        var result = await _service.CreateAsync(new CreateUserRequest("alice@example.com", "Alice"), default);

        Assert.Equal("User.EmailTaken", result.Error!.Code);
    }

    [Fact]
    public async Task Update_ToEmailOfAnotherUser_ReturnsEmailTakenAndKeepsData()
    {
        await _service.CreateAsync(new CreateUserRequest("a@example.com", "A"), default);
        var b = (await _service.CreateAsync(new CreateUserRequest("b@example.com", "B"), default)).Value!;

        var result = await _service.UpdateAsync(b.Id, new UpdateUserRequest("a@example.com", "B2"), default);

        Assert.Equal("User.EmailTaken", result.Error!.Code);
        var stored = (await _service.GetAsync(b.Id, default)).Value!;
        Assert.Equal("b@example.com", stored.Email);
        Assert.Equal("B", stored.FullName);
    }

    [Fact]
    public async Task Update_KeepingOwnEmail_Succeeds()
    {
        var a = (await _service.CreateAsync(new CreateUserRequest("a@example.com", "A"), default)).Value!;

        var result = await _service.UpdateAsync(a.Id, new UpdateUserRequest("A@example.com", "A2"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("A2", result.Value!.FullName);
        Assert.Equal(a.CreatedAt, result.Value.CreatedAt);
    }

    [Fact]
    public async Task GetUpdateDelete_UnknownId_ReturnNotFound()
    {
        var id = Guid.NewGuid();

        Assert.Equal("User.NotFound", (await _service.GetAsync(id, default)).Error!.Code);
        Assert.Equal("User.NotFound", (await _service.UpdateAsync(id, new UpdateUserRequest("a@b.com", "A"), default)).Error!.Code);
        Assert.Equal("User.NotFound", (await _service.DeleteAsync(id, default)).Error!.Code);
    }

    [Fact]
    public async Task Delete_ExistingUser_RemovesIt()
    {
        var a = (await _service.CreateAsync(new CreateUserRequest("a@example.com", "A"), default)).Value!;

        var result = await _service.DeleteAsync(a.Id, default);

        Assert.True(result.IsSuccess);
        Assert.Empty(_repository.Users);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_WithInvalidPaging_Fails(int page, int pageSize)
    {
        var result = await _service.ListAsync(page, pageSize, null, default);

        Assert.Equal("User.InvalidPaging", result.Error!.Code);
    }

    [Fact]
    public async Task List_ReturnsRequestedPageWithTotal()
    {
        for (var i = 0; i < 25; i++)
            await _service.CreateAsync(new CreateUserRequest($"u{i}@example.com", $"User {i}"), default);

        var result = await _service.ListAsync(2, 20, "  ", default);

        Assert.Equal(5, result.Value!.Items.Count);
        Assert.Equal(25, result.Value.TotalCount);
        Assert.Null(_repository.LastSearch);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];
        public Error? FailNextSaveWith { get; set; }
        public string? LastSearch { get; private set; }

        public Task<User?> FindAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

        public Task<bool> EmailExistsAsync(string email, Guid? exceptId, CancellationToken ct) =>
            Task.FromResult(Users.Any(u => u.Email == email && u.Id != exceptId));

        public Task<(IReadOnlyList<User> Items, int TotalCount)> ListAsync(int page, int pageSize, string? search, CancellationToken ct)
        {
            LastSearch = search;
            IReadOnlyList<User> items = Users.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, Users.Count));
        }

        public void Add(User user) => Users.Add(user);

        public void Remove(User user) => Users.Remove(user);

        public Task<Result> SaveChangesAsync(CancellationToken ct)
        {
            if (FailNextSaveWith is { } error)
            {
                FailNextSaveWith = null;
                Users.Clear();
                return Task.FromResult(Result.Failure(error));
            }

            return Task.FromResult(Result.Success());
        }
    }
}
