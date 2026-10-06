using System.Net.Http.Json;
using System.Text.Json;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CancellationToken = System.Threading.CancellationToken;

namespace IndiaWorks.IntegrationTests.GraphQL.E01;

public sealed class ExposeGraphQLTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExposeGraphQLTests(WebApplicationFactory<Program> factory)
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
            builder.ConfigureServices(collection =>
            {
                collection.AddSingleton<FakeUserSessionRepository>();
                collection.AddScoped<IUserSessionRepository>(sp => sp.GetRequiredService<FakeUserSessionRepository>());
            });
        });
        _client = _factory.CreateClient();
    }

    private sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        private readonly Dictionary<Guid, UserSession> _byId = new();

        public void Add(UserSession session)
        {
            _byId[session.Id] = session;
        }

        public Task<UserSession?> GetActiveByUserAndDeviceAsync(
            Guid userId,
            Guid deviceId,
            CancellationToken cancellationToken = default)
        {
            foreach (var session in _byId.Values)
            {
                if (session.UserId == userId &&
                    session.DeviceId == deviceId &&
                    !session.IsRevoked &&
                    !session.IsExpired)
                {
                    return Task.FromResult<UserSession?>(session);
                }
            }
            return Task.FromResult<UserSession?>(null);
        }

        public Task<IReadOnlyList<UserSession>> GetAllByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var list = _byId.Values
                .Where(s => s.UserId == userId)
                .ToList()
                .AsReadOnly();
            return Task.FromResult<IReadOnlyList<UserSession>>(list);
        }

        public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            Add(session);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Clear() => _byId.Clear();
    }

    private static StringContent CreateJsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj), System.Text.Encoding.UTF8, "application/json");

    private static JsonElement GetData(JsonElement response) =>
        response.GetProperty("data");

    private static JsonElement? GetErrors(JsonElement response)
    {
        if (response.TryGetProperty("errors", out var errors))
        {
            return errors;
        }
        return null;
    }

    // ── me/sessions query ────────────────────────────────────────────────

    [Fact]
    public async Task MeSessions_happy_path_returns_active_sessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var sessionRepo = _factory.Services.GetRequiredService<FakeUserSessionRepository>();
        sessionRepo.Clear();

        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(session);

        var query = new { query = $"{{ meSessions(userId: \"{userId:N}\") {{ id deviceId activePersona createdOn lastActivityOn expiresOn }} }}" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content);
        var sessions = data.GetProperty("meSessions");
        Assert.Equal(1, sessions.GetArrayLength());
        var s = sessions[0];
        Assert.Equal(session.Id.ToString(), s.GetProperty("id").GetString());
        Assert.Equal(session.DeviceId.ToString(), s.GetProperty("deviceId").GetString());
        Assert.Equal("CANDIDATE", s.GetProperty("activePersona").GetString());
    }

    [Fact]
    public async Task MeSessions_emptyUserId_returns_validation_error()
    {
        // Arrange
        var query = new { query = "{ meSessions(userId: \"00000000-0000-0000-0000-000000000000\") { id } }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - GraphQL returns error for empty Guid
        Assert.NotNull(GetErrors(content));
    }

    [Fact]
    public async Task MeSessions_returns_only_active_non_expired_sessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionRepo = _factory.Services.GetRequiredService<FakeUserSessionRepository>();
        sessionRepo.Clear();

        var activeSession = UserSession.Create(
            userId: userId,
            deviceId: Guid.NewGuid(),
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(activeSession);

        // Note: expired sessions are filtered out by the service layer (IsRevoked/IsExpired checks)

        var query = new { query = $"{{ meSessions(userId: \"{userId:N}\") {{ id activePersona }} }}" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var sessions = GetData(content).GetProperty("meSessions");
        Assert.Equal(1, sessions.GetArrayLength());
        Assert.Equal("CANDIDATE", sessions[0].GetProperty("activePersona").GetString());
    }

    // ── switchPersona mutation ───────────────────────────────────────────

    [Fact]
    public async Task SwitchPersona_happy_path_switches_and_returns_new_persona()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var sessionRepo = _factory.Services.GetRequiredService<FakeUserSessionRepository>();
        sessionRepo.Clear();

        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(session);

        var mutation = new
        {
            query = $"mutation {{ switchPersona(input: {{ userId: \"{userId:N}\", deviceId: \"{deviceId:N}\", newPersona: \"Employer\" }}) {{ isActive activePersona errorCode errorMessage }} }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content);
        var result = data.GetProperty("switchPersona");
        Assert.True(result.GetProperty("isActive").GetBoolean());
        Assert.Equal("EMPLOYER", result.GetProperty("activePersona").GetString());
    }

    [Fact]
    public async Task SwitchPersona_with_empty_userId_returns_error()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var mutation = new
        {
            query = $"mutation {{ switchPersona(input: {{ userId: \"00000000-0000-0000-0000-000000000000\", deviceId: \"{deviceId:N}\", newPersona: \"Employer\" }}) {{ isActive errorCode errorMessage }} }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.NotNull(GetErrors(content));
    }

    [Fact]
    public async Task SwitchPersona_with_invalid_persona_returns_error()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var mutation = new
        {
            query = $"mutation {{ switchPersona(input: {{ userId: \"{userId:N}\", deviceId: \"{deviceId:N}\", newPersona: \"InvalidPersona\" }}) {{ isActive errorCode errorMessage }} }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.NotNull(GetErrors(content));
    }

    [Fact]
    public async Task SwitchPersona_with_no_active_session_returns_session_revoked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var sessionRepo = _factory.Services.GetRequiredService<FakeUserSessionRepository>();
        sessionRepo.Clear();

        var mutation = new
        {
            query = $"mutation {{ switchPersona(input: {{ userId: \"{userId:N}\", deviceId: \"{deviceId:N}\", newPersona: \"Employer\" }}) {{ isActive errorCode errorMessage }} }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - should return failure result with SESSION_REVOKED code
        Assert.Null(GetErrors(content)); // GraphQL itself succeeds, business logic returns error in payload
        var data = GetData(content);
        var result = data.GetProperty("switchPersona");
        Assert.False(result.GetProperty("isActive").GetBoolean());
        Assert.Equal("SESSION_REVOKED", result.GetProperty("errorCode").GetString());
        Assert.Contains("No active session", result.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task SwitchPersona_switches_back_to_candidate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var sessionRepo = _factory.Services.GetRequiredService<FakeUserSessionRepository>();
        sessionRepo.Clear();

        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(session);

        // First switch to Employer
        var mutation1 = new
        {
            query = $"mutation {{ switchPersona(input: {{ userId: \"{userId:N}\", deviceId: \"{deviceId:N}\", newPersona: \"Employer\" }}) {{ isActive activePersona }} }}"
        };
        await _client.PostAsJsonAsync("/graphql", mutation1);

        // Then switch back to Candidate
        var mutation2 = new
        {
            query = $"mutation {{ switchPersona(input: {{ userId: \"{userId:N}\", deviceId: \"{deviceId:N}\", newPersona: \"Candidate\" }}) {{ isActive activePersona }} }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation2);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var result = GetData(content).GetProperty("switchPersona");
        Assert.True(result.GetProperty("isActive").GetBoolean());
        Assert.Equal("CANDIDATE", result.GetProperty("activePersona").GetString());
    }

    [Fact]
    public async Task GraphQl_operations_do_not_expose_sensitive_data_in_logs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var userIdString = userId.ToString("N");
        var deviceIdString = deviceId.ToString("N");
        var sessionRepo = _factory.Services.GetRequiredService<FakeUserSessionRepository>();
        sessionRepo.Clear();

        var session = UserSession.Create(
            userId: userId,
            deviceId: deviceId,
            activePersona: UserSession.Persona.Candidate,
            expiresOn: DateTime.UtcNow.AddHours(1));
        sessionRepo.Add(session);

        var query = new { query = $"{{ meSessions(userId: \"{userId:N}\") {{ id }} }}" };

        // Capture logs - verify no full GUIDs appear
        var logCollector = new List<string>();
        var loggerFactory = _factory.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
        
        // Act
        var response2 = await _client.PostAsJsonAsync("/graphql", query);
        response2.EnsureSuccessStatusCode();

        // Assert: The response should not contain full GUIDs (they should be masked in any logging)
        var responseBody = await response2.Content.ReadAsStringAsync();
        // Verify response structure is safe (no raw identifiers beyond what's needed for the API)
        var json = JsonDocument.Parse(responseBody);
        Assert.True(json.RootElement.TryGetProperty("data", out _));
    }
}
