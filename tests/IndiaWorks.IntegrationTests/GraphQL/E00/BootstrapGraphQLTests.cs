using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IndiaWorks.IntegrationTests.GraphQL.E00;

public sealed class BootstrapGraphQLTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BootstrapGraphQLTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:IndiaWorksDb"] = "Server=localhost;Database=IndiaWorks_Test;Trusted_Connection=True;TrustServerCertificate=True;"
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GraphQL_Endpoint_Returns_Version_Query()
    {
        // Arrange
        var query = new { query = "{ version }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = content.GetProperty("data");
        var version = data.GetProperty("version").GetString();
        Assert.Equal("v0.0.1", version);
    }

    [Fact]
    public async Task GraphQL_Endpoint_Returns_Health_Query()
    {
        // Arrange
        var query = new { query = "{ health }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = content.GetProperty("data");
        var health = data.GetProperty("health").GetString();
        Assert.Equal("OK", health);
    }

    [Fact]
    public async Task GraphQL_Endpoint_Returns_Echo_Mutation()
    {
        // Arrange
        var query = new { query = "mutation { echo(input: \"hello\") }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = content.GetProperty("data");
        var echo = data.GetProperty("echo").GetString();
        Assert.Equal("hello", echo);
    }

    [Fact]
    public async Task GraphQL_Endpoint_Handles_Invalid_Query_Gracefully()
    {
        // Arrange
        var query = new { query = "{ invalidField }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert - GraphQL returns 200 with errors in body, or 400 for completely invalid requests
        // Both are valid behaviors - check for errors in response if 200
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.TryGetProperty("errors", out _));
            var errors = content.GetProperty("errors");
            Assert.True(errors.GetArrayLength() > 0);
            var firstError = errors[0];
            Assert.Equal("INTERNAL_ERROR", firstError.GetProperty("extensions").GetProperty("code").GetString());
        }
        else
        {
            // 400 Bad Request is also acceptable for invalid queries
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
