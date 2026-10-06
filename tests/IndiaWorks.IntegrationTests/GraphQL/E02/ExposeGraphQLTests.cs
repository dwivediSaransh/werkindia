using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Domain.E02;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CancellationToken = System.Threading.CancellationToken;

namespace IndiaWorks.IntegrationTests.GraphQL.E02;

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
                collection.AddSingleton<FakeOrganisationRepository>();
                collection.AddSingleton<FakeOrganisationMembershipRepository>();
                collection.AddSingleton<FakeOrganisationInvitationRepository>();
                collection.AddScoped<IOrganisationRepository>(sp => sp.GetRequiredService<FakeOrganisationRepository>());
                collection.AddScoped<IOrganisationMembershipRepository>(sp => sp.GetRequiredService<FakeOrganisationMembershipRepository>());
                collection.AddScoped<IOrganisationInvitationRepository>(sp => sp.GetRequiredService<FakeOrganisationInvitationRepository>());
            });
        });
        _client = _factory.CreateClient();
    }

    private sealed class FakeOrganisationRepository : IOrganisationRepository
    {
        private readonly Dictionary<Guid, Organisation> _organisations = new();

        public Task AddAsync(Organisation organisation, CancellationToken cancellationToken = default)
        {
            _organisations[organisation.Id] = organisation;
            return Task.CompletedTask;
        }

        public Task<Organisation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _organisations.TryGetValue(id, out var org);
            return Task.FromResult(org);
        }

        public Task<IReadOnlyList<Organisation>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        {
            var organisations = ids
                .Where(_organisations.ContainsKey)
                .Select(id => _organisations[id])
                .ToList();

            return Task.FromResult<IReadOnlyList<Organisation>>(organisations);
        }

        public Task<IReadOnlyList<Organisation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Organisation>>(_organisations.Values.ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeOrganisationMembershipRepository : IOrganisationMembershipRepository
    {
        private readonly List<OrganisationMembership> _memberships = new();

        public Task AddAsync(OrganisationMembership membership, CancellationToken cancellationToken = default)
        {
            _memberships.Add(membership);
            return Task.CompletedTask;
        }

        public Task<OrganisationMembership?> GetAsync(Guid organisationId, Guid userAccountId, CancellationToken cancellationToken = default)
        {
            var m = _memberships.FirstOrDefault(x => x.OrganisationId == organisationId && x.UserAccountId == userAccountId);
            return Task.FromResult(m);
        }

        public Task<IReadOnlyList<OrganisationMembership>> GetByUserAccountIdAsync(Guid userAccountId, CancellationToken cancellationToken = default)
        {
            var list = _memberships.Where(x => x.UserAccountId == userAccountId).ToList();
            return Task.FromResult<IReadOnlyList<OrganisationMembership>>(list);
        }

        public Task<IReadOnlyList<OrganisationMembership>> GetByOrganisationIdAsync(Guid organisationId, CancellationToken cancellationToken = default)
        {
            var list = _memberships.Where(x => x.OrganisationId == organisationId).ToList();
            return Task.FromResult<IReadOnlyList<OrganisationMembership>>(list);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeOrganisationInvitationRepository : IOrganisationInvitationRepository
    {
        private readonly List<OrganisationInvitation> _invitations = new();

        public Task AddAsync(OrganisationInvitation invitation, CancellationToken cancellationToken = default)
        {
            _invitations.Add(invitation);
            return Task.CompletedTask;
        }

        public Task<OrganisationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var inv = _invitations.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(inv);
        }

        public Task<IReadOnlyList<OrganisationInvitation>> GetByOrganisationIdAsync(Guid organisationId, CancellationToken cancellationToken = default)
        {
            var list = _invitations.Where(x => x.OrganisationId == organisationId).ToList();
            return Task.FromResult<IReadOnlyList<OrganisationInvitation>>(list);
        }

        public Task<OrganisationInvitation?> GetPendingByOrgAndInviteeHashAsync(Guid organisationId, byte[] inviteeIdentifierHash, CancellationToken cancellationToken = default)
        {
            var inv = _invitations.FirstOrDefault(x =>
                x.OrganisationId == organisationId &&
                x.Status == InvitationStatus.Pending &&
                x.InviteeIdentifierHash.SequenceEqual(inviteeIdentifierHash));
            return Task.FromResult(inv);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static StringContent CreateJsonContent(object obj) =>
        new(JsonSerializer.Serialize(obj), System.Text.Encoding.UTF8, "application/json");

    private static JsonElement GetData(JsonElement response) => response.GetProperty("data");

    private static JsonElement? GetErrors(JsonElement response)
    {
        if (response.TryGetProperty("errors", out var errors))
        {
            return errors;
        }
        return null;
    }

    // ── CreateOrganisation mutation ───────────────────────────────────────

    [Fact]
    public async Task CreateOrganisation_happy_path_creates_organisation_and_owner_membership()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var mutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{creatorId:N}"",
                        legalName: ""Acme Technologies Pvt Ltd"",
                        tradeName: ""Acme Tech"",
                        sector: Private,
                        employeeBand: ""51-200"",
                        organisationType: ""Private Limited"",
                        nic2025Code: ""6201""
                    }}) {{
                        isActive
                        organisationId
                        legalName
                        tradeName
                        sector
                        employeeBand
                        role
                        createdAt
                        errorCode
                        errorMessage
                    }}
                }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content);
        var result = data.GetProperty("createOrganisation");
        Assert.True(result.GetProperty("isActive").GetBoolean());
        Assert.NotEqual(Guid.Empty, Guid.Parse(result.GetProperty("organisationId").GetString()!));
        Assert.Equal("Acme Technologies Pvt Ltd", result.GetProperty("legalName").GetString());
        Assert.Equal("Acme Tech", result.GetProperty("tradeName").GetString());
        Assert.Equal("Private", result.GetProperty("sector").GetString());
        Assert.Equal("51-200", result.GetProperty("employeeBand").GetString());
        Assert.Equal("Owner", result.GetProperty("role").GetString());
        Assert.Null(result.GetProperty("errorCode").GetString());
        Assert.Null(result.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task CreateOrganisation_empty_creatorId_returns_validation_error()
    {
        // Arrange
        var mutation = new
        {
            query = @"
                mutation {
                    createOrganisation(input: {
                        creatorUserAccountId: ""00000000-0000-0000-0000-000000000000"",
                        legalName: ""Valid Legal Name""
                    }) {
                        isActive
                        errorCode
                        errorMessage
                    }
                }"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var result = GetData(content).GetProperty("createOrganisation");
        Assert.False(result.GetProperty("isActive").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", result.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task CreateOrganisation_empty_legalName_returns_validation_error()
    {
        // Arrange
        var mutation = new
        {
            query = @"
                mutation {
                    createOrganisation(input: {
                        creatorUserAccountId: ""00000000-0000-0000-0000-000000000001"",
                        legalName: """"
                    }) {
                        isActive
                        errorCode
                        errorMessage
                    }
                }"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var result = GetData(content).GetProperty("createOrganisation");
        Assert.False(result.GetProperty("isActive").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", result.GetProperty("errorCode").GetString());
    }

    // ── MyOrganisations query ──────────────────────────────────────────────

    [Fact]
    public async Task MyOrganisations_happy_path_returns_user_organisations_with_roles()
    {
        // Arrange - seed data via mutation first
        var userId = Guid.NewGuid();

        // Create organisation first
        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{userId:N}"",
                        legalName: ""Seed Organisation""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var createData = GetData(createContent).GetProperty("createOrganisation");
        var createdOrgId = Guid.Parse(createData.GetProperty("organisationId").GetString()!);

        // Act - query myOrganisations
        var query = new
        {
            query = $@"
                {{
                    myOrganisations(userId: ""{userId:N}"") {{
                        id
                        legalName
                        tradeName
                        sector
                        employeeBand
                        organisationType
                        nic2025Code
                        verificationStatus
                        createdAt
                        role
                    }}
                }}"
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content);
        var orgs = data.GetProperty("myOrganisations");
        Assert.Equal(1, orgs.GetArrayLength());
        var org = orgs[0];
        Assert.Equal(createdOrgId.ToString(), org.GetProperty("id").GetString());
        Assert.Equal("Seed Organisation", org.GetProperty("legalName").GetString());
        Assert.Null(org.GetProperty("tradeName").GetString());
        Assert.Equal("Unverified", org.GetProperty("verificationStatus").GetString());
        Assert.Equal("Owner", org.GetProperty("role").GetString());
    }

    [Fact]
    public async Task MyOrganisations_emptyUserId_returns_validation_error()
    {
        // Arrange
        var query = new { query = "{ myOrganisations(userId: \"00000000-0000-0000-0000-000000000000\") { id } }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - GraphQL returns the stable validation code for empty Guid.
        var errors = GetErrors(content);
        Assert.NotNull(errors);
        Assert.Equal("VALIDATION_FAILED", errors.Value[0].GetProperty("extensions").GetProperty("code").GetString());
    }

    [Fact]
    public async Task MyOrganisations_userWithNoOrganisations_returns_emptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new
        {
            query = $@"
                {{
                    myOrganisations(userId: ""{userId:N}"") {{
                        id
                        legalName
                    }}
                }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var orgs = GetData(content).GetProperty("myOrganisations");
        Assert.Equal(0, orgs.GetArrayLength());
    }

    // ── Invitation operations ──────────────────────────────────────────────

    [Fact]
    public async Task InviteMember_happy_path_creates_pending_invitation()
    {
        // Arrange - create organisation + owner membership
        var creatorId = Guid.NewGuid();
        var inviteeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("invitee@example.com"));
        var tokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("secure-token-123"));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{creatorId:N}"",
                        legalName: ""Invite Test Org"",
                        tradeName: ""Invite Test"",
                        sector: Private,
                        employeeBand: ""51-200"",
                        organisationType: ""Private Limited"",
                        nic2025Code: ""6201""
                    }}) {{
                        isActive
                        organisationId
                        role
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var createData = GetData(createContent).GetProperty("createOrganisation");
        var createdOrgId = Guid.Parse(createData.GetProperty("organisationId").GetString()!);
        Assert.Equal("Owner", createData.GetProperty("role").GetString());

        // Act - invite a member
        var inviteeHashList = string.Join(", ", inviteeHash.Select(b => (int)b));
        var tokenHashList = string.Join(", ", tokenHash.Select(b => (int)b));
        var inviteMutation = new
        {
            query = $@"
                mutation {{
                    inviteMember(input: {{
                        organisationId: ""{createdOrgId:N}"",
                        invitedByUserAccountId: ""{creatorId:N}"",
                        inviteeIdentifierType: ""Email"",
                        inviteeIdentifierHash: [{inviteeHashList}],
                        role: ""Viewer"",
                        tokenHash: [{tokenHashList}],
                        expiresAt: ""{expiresAt:O}""
                    }}) {{
                        isSuccess
                        invitationId
                        errorCode
                        errorMessage
                    }}
                }}"
        };
        var inviteResponse = await _client.PostAsJsonAsync("/graphql", inviteMutation);
        inviteResponse.EnsureSuccessStatusCode();
        var inviteContent = await inviteResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(inviteContent));
        var inviteData = GetData(inviteContent).GetProperty("inviteMember");
        Assert.True(inviteData.GetProperty("isSuccess").GetBoolean());
        Assert.NotEqual(Guid.Empty, Guid.Parse(inviteData.GetProperty("invitationId").GetString()!));
        Assert.Null(inviteData.GetProperty("errorCode").GetString());
        Assert.Null(inviteData.GetProperty("errorMessage").GetString());

        // Verify pending invitation appears in list
        var listQuery = new
        {
            query = $@"
                {{
                    pendingInvitations(organisationId: ""{createdOrgId:N}"", callerUserAccountId: ""{creatorId:N}"") {{
                        id
                        organisationId
                        role
                        status
                        expiresAt
                    }}
                }}"
        };
        var listResponse = await _client.PostAsJsonAsync("/graphql", listQuery);
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Null(GetErrors(listContent));
        var listData = GetData(listContent).GetProperty("pendingInvitations");
        Assert.Equal(1, listData.GetArrayLength());
        Assert.Equal(createdOrgId.ToString(), listData[0].GetProperty("organisationId").GetString());
        Assert.Equal("Viewer", listData[0].GetProperty("role").GetString());
        Assert.Equal("Pending", listData[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task InviteMember_non_owner_recruiter_cannot_invite()
    {
        // Arrange - create organisation with Owner and a Recruiter
        var ownerId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var inviteeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("other@example.com"));
        var tokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("token-456"));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{ownerId:N}"",
                        legalName: ""Recruiter Test Org""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var createdOrgId = Guid.Parse(GetData(createContent).GetProperty("createOrganisation").GetProperty("organisationId").GetString()!);

        // Add a recruiter membership via direct service call (seed data)
        using var scope = _factory.Services.CreateScope();
        var membershipRepo = scope.ServiceProvider.GetRequiredService<IOrganisationMembershipRepository>();
        await membershipRepo.AddAsync(OrganisationMembership.Create(
            organisationId: createdOrgId,
            userAccountId: recruiterId,
            role: OrganisationRole.Recruiter), CancellationToken.None);
        await membershipRepo.SaveChangesAsync(CancellationToken.None);

        // Act - recruiter tries to invite
        var inviteeHashList = string.Join(", ", inviteeHash.Select(b => (int)b));
        var tokenHashList = string.Join(", ", tokenHash.Select(b => (int)b));
        var inviteMutation = new
        {
            query = $@"
                mutation {{
                    inviteMember(input: {{
                        organisationId: ""{createdOrgId:N}"",
                        invitedByUserAccountId: ""{recruiterId:N}"",
                        inviteeIdentifierType: ""Email"",
                        inviteeIdentifierHash: [{inviteeHashList}],
                        role: ""Viewer"",
                        tokenHash: [{tokenHashList}],
                        expiresAt: ""{expiresAt:O}""
                    }}) {{
                        isSuccess
                        errorCode
                        errorMessage
                    }}
                }}"
        };
        var response = await _client.PostAsJsonAsync("/graphql", inviteMutation);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - role not allowed
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("inviteMember");
        Assert.False(data.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("ROLE_NOT_ALLOWED", data.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task InviteMember_empty_organisationId_returns_validation_error()
    {
        // Arrange - use byte array format for hash fields
        var inviteeHash = new byte[] { 0x61, 0x62, 0x63 }; // "abc"
        var tokenHash = new byte[] { 0x74, 0x6F, 0x6B, 0x65, 0x6E }; // "token"
        var inviteeHashList = string.Join(", ", inviteeHash.Select(b => (int)b));
        var tokenHashList = string.Join(", ", tokenHash.Select(b => (int)b));

        var mutation = new
        {
            query = $@"
                mutation {{
                    inviteMember(input: {{
                        organisationId: ""00000000-0000-0000-0000-000000000000"",
                        invitedByUserAccountId: ""00000000-0000-0000-0000-000000000001"",
                        inviteeIdentifierType: ""Email"",
                        inviteeIdentifierHash: [{inviteeHashList}],
                        role: ""Viewer"",
                        tokenHash: [{tokenHashList}],
                        expiresAt: ""2099-01-01T00:00:00Z""
                    }}) {{
                        isSuccess
                        errorCode
                    }}
                }}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var result = GetData(content).GetProperty("inviteMember");
        Assert.False(result.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", result.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task PendingInvitations_happy_path_returns_only_pending_invitations()
    {
        // Arrange - create organisation + owner, then invite two members
        var ownerId = Guid.NewGuid();
        var hash1 = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("user1@example.com"));
        var hash2 = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("user2@example.com"));
        var tokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("token"));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{ownerId:N}"",
                        legalName: ""Pending Test Org""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createdOrgId = Guid.Parse(GetData(await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("createOrganisation").GetProperty("organisationId").GetString()!);

        // Invite first member
        await InviteMemberAsync(createdOrgId, ownerId, "Email", hash1, "Viewer", tokenHash, expiresAt);

        // Invite second member
        await InviteMemberAsync(createdOrgId, ownerId, "Email", hash2, "Recruiter", tokenHash, expiresAt);

        // Act - list pending invitations
        var query = new
        {
            query = $@"
                {{
                    pendingInvitations(organisationId: ""{createdOrgId:N}"", callerUserAccountId: ""{ownerId:N}"") {{
                        id
                        role
                        status
                    }}
                }}"
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("pendingInvitations");
        Assert.Equal(2, data.GetArrayLength());
    }

    [Fact]
    public async Task PendingInvitations_non_member_returns_error_via_graphql()
    {
        // Arrange - create organisation + owner
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{ownerId:N}"",
                        legalName: ""Private Org""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createdOrgId = Guid.Parse(GetData(await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("createOrganisation").GetProperty("organisationId").GetString()!);

        // Act - outsider tries to list pending invitations
        var query = new
        {
            query = $@"
                {{
                    pendingInvitations(organisationId: ""{createdOrgId:N}"", callerUserAccountId: ""{outsiderId:N}"") {{
                        id
                    }}
                }}"
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert - GraphQL returns an error (not a successful response with data)
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = GetErrors(content);
        Assert.NotNull(errors);
        Assert.True(errors.Value.GetArrayLength() >= 1);
        var error = errors.Value[0];
        // Error message should indicate authorization failure
        var message = error.GetProperty("message").GetString();
        Assert.Contains("Owner", message);
        Assert.Contains("Admin", message);
    }

    [Fact]
    public async Task AcceptInvitation_happy_path_activates_membership()
    {
        // Arrange - create organisation + owner, invite a viewer, then accept
        var ownerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var inviteeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("invitee@example.com"));
        var tokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("accept-token"));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{ownerId:N}"",
                        legalName: ""Accept Test Org""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createdOrgId = Guid.Parse(GetData(await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("createOrganisation").GetProperty("organisationId").GetString()!);

        // Invite
        var inviteResult = await InviteMemberAsync(createdOrgId, ownerId, "Email", inviteeHash, "Viewer", tokenHash, expiresAt);
        var invitationId = inviteResult.InvitationId;

        // Act - accept invitation
        var acceptMutation = new
        {
            query = $@"
                mutation {{
                    acceptInvitation(input: {{
                        invitationId: ""{invitationId:N}"",
                        acceptedByUserAccountId: ""{inviteeId:N}""
                    }}) {{
                        isSuccess
                        organisationId
                        role
                        errorCode
                        errorMessage
                    }}
                }}"
        };
        var acceptResponse = await _client.PostAsJsonAsync("/graphql", acceptMutation);
        acceptResponse.EnsureSuccessStatusCode();
        var acceptContent = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(acceptContent));
        var acceptData = GetData(acceptContent).GetProperty("acceptInvitation");
        Assert.True(acceptData.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(createdOrgId.ToString(), acceptData.GetProperty("organisationId").GetString());
        Assert.Equal("Viewer", acceptData.GetProperty("role").GetString());
        Assert.Null(acceptData.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task AcceptInvitation_already_active_member_is_idempotent()
    {
        // Arrange - create organisation, invite, accept once
        var ownerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var inviteeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("idem@example.com"));
        var tokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("idem-token"));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{ownerId:N}"",
                        legalName: ""Idempotent Org""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createdOrgId = Guid.Parse(GetData(await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("createOrganisation").GetProperty("organisationId").GetString()!);

        var inviteResult = await InviteMemberAsync(createdOrgId, ownerId, "Email", inviteeHash, "Viewer", tokenHash, expiresAt);
        var invitationId = inviteResult.InvitationId;

        // First accept
        var firstAccept = new
        {
            query = $@"
                mutation {{
                    acceptInvitation(input: {{
                        invitationId: ""{invitationId:N}"",
                        acceptedByUserAccountId: ""{inviteeId:N}""
                    }}) {{
                        isSuccess
                        organisationId
                        role
                    }}
                }}"
        };
        var firstResponse = await _client.PostAsJsonAsync("/graphql", firstAccept);
        firstResponse.EnsureSuccessStatusCode();
        var firstContent = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstData = GetData(firstContent).GetProperty("acceptInvitation");
        Assert.True(firstData.GetProperty("isSuccess").GetBoolean());

        // Act - accept again (idempotent)
        var secondAccept = new
        {
            query = $@"
                mutation {{
                    acceptInvitation(input: {{
                        invitationId: ""{invitationId:N}"",
                        acceptedByUserAccountId: ""{inviteeId:N}""
                    }}) {{
                        isSuccess
                        organisationId
                        role
                    }}
                }}"
        };
        var secondResponse = await _client.PostAsJsonAsync("/graphql", secondAccept);
        secondResponse.EnsureSuccessStatusCode();
        var secondContent = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - still succeeds (idempotent)
        Assert.Null(GetErrors(secondContent));
        var secondData = GetData(secondContent).GetProperty("acceptInvitation");
        Assert.True(secondData.GetProperty("isSuccess").GetBoolean());
        Assert.Equal(createdOrgId.ToString(), secondData.GetProperty("organisationId").GetString());
    }

    [Fact]
    public async Task AcceptInvitation_empty_invitationId_returns_validation_error()
    {
        // Arrange
        var mutation = new
        {
            query = @"
                mutation {
                    acceptInvitation(input: {
                        invitationId: ""00000000-0000-0000-0000-000000000000"",
                        acceptedByUserAccountId: ""00000000-0000-0000-0000-000000000001""
                    }) {
                        isSuccess
                        errorCode
                    }
                }"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var result = GetData(content).GetProperty("acceptInvitation");
        Assert.False(result.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", result.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task RevokeInvitation_happy_path_revokes_pending_invitation()
    {
        // Arrange - create organisation + owner, invite a member
        var ownerId = Guid.NewGuid();
        var inviteeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("revoke@example.com"));
        var tokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("revoke-token"));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{ownerId:N}"",
                        legalName: ""Revoke Test Org""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createdOrgId = Guid.Parse(GetData(await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("createOrganisation").GetProperty("organisationId").GetString()!);

        var inviteResult = await InviteMemberAsync(createdOrgId, ownerId, "Email", inviteeHash, "Viewer", tokenHash, expiresAt);
        var invitationId = inviteResult.InvitationId;

        // Act - revoke the invitation
        var revokeMutation = new
        {
            query = $@"
                mutation {{
                    revokeInvitation(input: {{
                        invitationId: ""{invitationId:N}"",
                        revokedByUserAccountId: ""{ownerId:N}""
                    }}) {{
                        isSuccess
                        errorCode
                        errorMessage
                    }}
                }}"
        };
        var revokeResponse = await _client.PostAsJsonAsync("/graphql", revokeMutation);
        revokeResponse.EnsureSuccessStatusCode();
        var revokeContent = await revokeResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(revokeContent));
        var revokeData = GetData(revokeContent).GetProperty("revokeInvitation");
        Assert.True(revokeData.GetProperty("isSuccess").GetBoolean());
        Assert.Null(revokeData.GetProperty("errorCode").GetString());

        // Verify invitation no longer appears in pending list
        var listQuery = new
        {
            query = $@"
                {{
                    pendingInvitations(organisationId: ""{createdOrgId:N}"", callerUserAccountId: ""{ownerId:N}"") {{
                        id
                        status
                    }}
                }}"
        };
        var listResponse = await _client.PostAsJsonAsync("/graphql", listQuery);
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var listData = GetData(listContent).GetProperty("pendingInvitations");
        Assert.Equal(0, listData.GetArrayLength());
    }

    [Fact]
    public async Task RevokeInvitation_non_owner_cannot_revoke()
    {
        // Arrange - create organisation with Owner and a Viewer member (who accepted their invite)
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var inviteeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("viewer@example.com"));
        var tokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("viewer-token"));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{ownerId:N}"",
                        legalName: ""Revoke Auth Test Org""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createdOrgId = Guid.Parse(GetData(await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("createOrganisation").GetProperty("organisationId").GetString()!);

        // Invite and accept as viewer to make them an active member
        var inviteResult = await InviteMemberAsync(createdOrgId, ownerId, "Email", inviteeHash, "Viewer", tokenHash, expiresAt);
        var invitationId = inviteResult.InvitationId;

        // Accept the invitation to become an active member
        var acceptMutation = new
        {
            query = $@"
                mutation {{
                    acceptInvitation(input: {{
                        invitationId: ""{invitationId:N}"",
                        acceptedByUserAccountId: ""{viewerId:N}""
                    }}) {{
                        isSuccess
                    }}
                }}"
        };
        await _client.PostAsJsonAsync("/graphql", acceptMutation);

        // Now invite a second person
        var secondInviteeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("second@example.com"));
        var secondTokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("second-token"));
        var secondInviteResult = await InviteMemberAsync(createdOrgId, ownerId, "Email", secondInviteeHash, "Viewer", secondTokenHash, expiresAt);
        var secondInvitationId = secondInviteResult.InvitationId;

        // Act - viewer (non-owner) tries to revoke the second invitation
        var revokeMutation = new
        {
            query = $@"
                mutation {{
                    revokeInvitation(input: {{
                        invitationId: ""{secondInvitationId:N}"",
                        revokedByUserAccountId: ""{viewerId:N}""
                    }}) {{
                        isSuccess
                        errorCode
                        errorMessage
                    }}
                }}"
        };
        var response = await _client.PostAsJsonAsync("/graphql", revokeMutation);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - role not allowed
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("revokeInvitation");
        Assert.False(data.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("ROLE_NOT_ALLOWED", data.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task RevokeInvitation_empty_invitationId_returns_validation_error()
    {
        // Arrange
        var mutation = new
        {
            query = @"
                mutation {
                    revokeInvitation(input: {
                        invitationId: ""00000000-0000-0000-0000-000000000000"",
                        revokedByUserAccountId: ""00000000-0000-0000-0000-000000000001""
                    }) {
                        isSuccess
                        errorCode
                    }
                }"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var result = GetData(content).GetProperty("revokeInvitation");
        Assert.False(result.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("VALIDATION_FAILED", result.GetProperty("errorCode").GetString());
    }

    // ── Sensitive data privacy check ───────────────────────────────────────

    [Fact]
    public async Task Invitation_operations_do_not_expose_sensitive_data_in_response()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var inviteeHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("privacy@example.com"));
        var tokenHash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("privacy-token"));
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var createMutation = new
        {
            query = $@"
                mutation {{
                    createOrganisation(input: {{
                        creatorUserAccountId: ""{ownerId:N}"",
                        legalName: ""Privacy Test Org""
                    }}) {{
                        isActive
                        organisationId
                    }}
                }}"
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", createMutation);
        createResponse.EnsureSuccessStatusCode();
        var createdOrgId = Guid.Parse(GetData(await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("createOrganisation").GetProperty("organisationId").GetString()!);

        var inviteeHashList = string.Join(", ", inviteeHash.Select(b => (int)b));
        var tokenHashList = string.Join(", ", tokenHash.Select(b => (int)b));
        var inviteMutation = new
        {
            query = $@"
                mutation {{
                    inviteMember(input: {{
                        organisationId: ""{createdOrgId:N}"",
                        invitedByUserAccountId: ""{ownerId:N}"",
                        inviteeIdentifierType: ""Email"",
                        inviteeIdentifierHash: [{inviteeHashList}],
                        role: ""Viewer"",
                        tokenHash: [{tokenHashList}],
                        expiresAt: ""{expiresAt:O}""
                    }}) {{
                        isSuccess
                        invitationId
                    }}
                }}"
        };
        await _client.PostAsJsonAsync("/graphql", inviteMutation);

        // Act - fetch pending invitations
        var query = new
        {
            query = $@"
                {{
                    pendingInvitations(organisationId: ""{createdOrgId:N}"", callerUserAccountId: ""{ownerId:N}"") {{
                        id
                        organisationId
                        inviteeIdentifierType
                        role
                        status
                        expiresAt
                        createdAt
                    }}
                }}"
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert: Response must not contain sensitive fields
        var json = JsonDocument.Parse(responseBody);
        Assert.True(json.RootElement.TryGetProperty("data", out _));
        var data = json.RootElement.GetProperty("data");
        if (data.TryGetProperty("pendingInvitations", out var invitations))
        {
            var existingProps = invitations[0].EnumerateObject().ToList();
            var propNames = existingProps.Select(p => p.Name).ToList();
            Assert.DoesNotContain("token", propNames);
            Assert.DoesNotContain("tokenHash", propNames);
            Assert.DoesNotContain("inviteeIdentifierHash", propNames);
            Assert.DoesNotContain("password", propNames);
            Assert.DoesNotContain("secret", propNames);
            Assert.DoesNotContain("aadhaar", propNames);
            Assert.DoesNotContain("pan", propNames);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<InviteMemberResultDto> InviteMemberAsync(
        Guid organisationId,
        Guid invitedByUserAccountId,
        string inviteeIdentifierType,
        byte[] inviteeIdentifierHash,
        string role,
        byte[] tokenHash,
        DateTime expiresAt)
    {
        // Convert byte arrays to GraphQL int list format [0, 1, 2, ...]
        var inviteeHashList = string.Join(", ", inviteeIdentifierHash.Select(b => (int)b));
        var tokenHashList = string.Join(", ", tokenHash.Select(b => (int)b));

        var mutation = new
        {
            query = $@"
                mutation {{
                    inviteMember(input: {{
                        organisationId: ""{organisationId:N}"",
                        invitedByUserAccountId: ""{invitedByUserAccountId:N}"",
                        inviteeIdentifierType: ""{inviteeIdentifierType}"",
                        inviteeIdentifierHash: [{inviteeHashList}],
                        role: ""{role}"",
                        tokenHash: [{tokenHashList}],
                        expiresAt: ""{expiresAt:O}""
                    }}) {{
                        isSuccess
                        invitationId
                    }}
                }}"
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = GetData(content).GetProperty("inviteMember");
        return new InviteMemberResultDto
        {
            IsSuccess = data.GetProperty("isSuccess").GetBoolean(),
            InvitationId = Guid.Parse(data.GetProperty("invitationId").GetString()!)
        };
    }

    private sealed class InviteMemberResultDto
    {
        public bool IsSuccess { get; set; }
        public Guid InvitationId { get; set; }
    }
}
