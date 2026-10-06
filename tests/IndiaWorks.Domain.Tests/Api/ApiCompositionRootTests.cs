using System.Text.Json;

namespace IndiaWorks.Domain.Tests.Api;

public sealed class ApiCompositionRootTests
{
    [Fact]
    public void Api_composition_root_wires_operational_foundation()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "src", "backend", "IndiaWorks.Api", "Program.cs"));

        Assert.Contains("AddOptions<IndiaWorksApiOptions>", program);
        Assert.Contains("AddProblemDetails", program);
        Assert.Contains("ConfigureHttpJsonOptions", program);
        Assert.Contains("AddHealthChecks", program);
        Assert.Contains("AddSimpleConsole", program);
        Assert.Contains("AddIndiaWorksApplication", program);
        Assert.Contains("AddIndiaWorksInfrastructure", program);
        Assert.Contains("AddGraphQLServer", program);
        Assert.Contains("AddQueryType<Query>", program);
        Assert.Contains("AddMutationType<Mutation>", program);
        Assert.Contains("AddErrorFilter<GraphQLErrorFilter>", program);
        Assert.Contains("MapGraphQL(\"/graphql\")", program);
        Assert.Contains("MapNitroApp(\"/graphql/ui\", \"/graphql\")", program);
        Assert.Contains("MapHealthChecks(\"/health\")", program);
        Assert.Contains("MapGet(\"/ready\"", program);
        Assert.Contains("MapGet(\"/health/ready\"", program);

        using var appSettings = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "src", "backend", "IndiaWorks.Api", "appsettings.json")));
        var indiaWorks = appSettings.RootElement.GetProperty("IndiaWorks");
        Assert.Equal("IndiaWorks", indiaWorks.GetProperty("ServiceName").GetString());
        Assert.Equal("Unified SaaS", indiaWorks.GetProperty("ArchitectureBaseline").GetString());
    }

    [Fact]
    public void Infrastructure_composition_uses_sql_server_provider()
    {
        var root = FindRepositoryRoot();
        var dependencyInjection = File.ReadAllText(Path.Combine(
            root,
            "src",
            "backend",
            "IndiaWorks.Infrastructure",
            "DependencyInjection.cs"));
        var designTimeFactory = File.ReadAllText(Path.Combine(
            root,
            "src",
            "backend",
            "IndiaWorks.Infrastructure",
            "Persistence",
            "AppDbContextDesignTimeFactory.cs"));

        Assert.Contains("GetConnectionString(\"IndiaWorksDb\")", dependencyInjection);
        Assert.Contains("UseSqlServer", dependencyInjection);
        Assert.Contains("IDesignTimeDbContextFactory<AppDbContext>", designTimeFactory);
        Assert.Contains("UseSqlServer", designTimeFactory);
        Assert.Contains("MigrationsAssembly", designTimeFactory);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IndiaWorks.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate IndiaWorks.slnx from test output directory.");
    }
}
