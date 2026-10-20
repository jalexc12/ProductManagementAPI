using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;
using Xunit;

namespace ProductManagementApi.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server integration";
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly object _startLock = new();
    private Task? _startTask;

    private readonly MsSqlContainer _container = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("A_Strong_Integration_Password123!")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task StartAsync()
    {
        lock (_startLock)
        {
            return _startTask ??= _container.StartAsync();
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() =>
        _startTask is null
            ? Task.CompletedTask
            : _container.DisposeAsync().AsTask();
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_SQLSERVER_INTEGRATION_TESTS") != "1")
        {
            Skip = "Set RUN_SQLSERVER_INTEGRATION_TESTS=1 and run with Docker available.";
        }
    }
}
