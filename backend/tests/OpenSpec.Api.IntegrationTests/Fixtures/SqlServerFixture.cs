using Testcontainers.MsSql;

namespace OpenSpec.Api.IntegrationTests.Fixtures;

/// <summary>SQL Server thật trong Docker cho các test cần DB. Dùng qua IClassFixture&lt;SqlServerFixture&gt;.</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
