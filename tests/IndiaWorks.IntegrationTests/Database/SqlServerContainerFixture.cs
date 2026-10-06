using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database;

/// <summary>
/// SQL Server Testcontainers fixture for integration tests.
/// Provides a real SQL Server container that starts once per test class.
/// Uses the base Testcontainers ContainerBuilder with the SQL Server Docker image;
/// no dedicated SqlServer module package is required.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private IContainer? _container;
    public string ConnectionString { get; private set; } = default!;
    public string DatabaseName { get; private set; } = default!;
    public bool IsAvailable { get; private set; }

    public SqlServerContainerFixture()
    {
        DatabaseName = $"IndiaWorks_Test_{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new ContainerBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-CU16-ubuntu22.04")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_SA_PASSWORD", "TestContainer!Pass99")
                .WithPortBinding(1433, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
                .Build();

            await _container.StartAsync();

            var port = _container.GetMappedPublicPort(1433);
            var host = _container.Hostname;
            ConnectionString = $"Server={host},{port};Database={DatabaseName};User Id=sa;Password=TestContainer!Pass99;TrustServerCertificate=True;";
            IsAvailable = true;
        }
        catch (Exception)
        {
            IsAvailable = false;
            ConnectionString = string.Empty;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            if (_container.State.HasFlag(TestcontainersStates.Running))
            {
                await _container.StopAsync();
            }
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a SqlConnection for manual database operations.
    /// </summary>
    public SqlConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }
}
