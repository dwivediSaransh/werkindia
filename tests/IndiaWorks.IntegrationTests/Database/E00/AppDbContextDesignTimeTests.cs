using IndiaWorks.Domain.E00;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace IndiaWorks.IntegrationTests.Database.E00;

public sealed class AppDbContextDesignTimeTests
{
    [Fact]
    public async Task DesignTimeFactory_AppliesMigrations_And_DatabaseRejectsDuplicatePrimaryKey()
    {
        var databaseName = $"IndiaWorks_Test_{Guid.NewGuid():N}";
        var connectionString = $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";
        var previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__IndiaWorksDb");

        Environment.SetEnvironmentVariable("ConnectionStrings__IndiaWorksDb", connectionString);
        await using var context = new AppDbContextDesignTimeFactory().CreateDbContext([]);

        try
        {
            Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);

            await context.Database.MigrateAsync();

            // After migrations, the model must match the current context exactly;
            // pending model changes would have already thrown above.
            context.ExampleEntities.Add(new ExampleEntity { Name = "valid" });
            await context.SaveChangesAsync();

            var duplicateInsert = """
                SET IDENTITY_INSERT [ExampleEntities] ON;
                INSERT INTO [ExampleEntities] ([Id], [Name]) VALUES (1, N'duplicate');
                SET IDENTITY_INSERT [ExampleEntities] OFF;
                """;

            await Assert.ThrowsAnyAsync<Exception>(() => context.Database.ExecuteSqlRawAsync(duplicateInsert));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
            Environment.SetEnvironmentVariable("ConnectionStrings__IndiaWorksDb", previousConnectionString);
        }
    }
}
