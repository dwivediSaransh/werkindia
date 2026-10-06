using System.Text.Json;
using System.Net.Http.Json;
using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Application.Features.E03.Implement;
using IndiaWorks.Domain.E02;
using IndiaWorks.Domain.E03;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CancellationToken = System.Threading.CancellationToken;

namespace IndiaWorks.IntegrationTests.GraphQL.E03;

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
                collection.AddSingleton<FakeTaxonomyLookupRepository>();
                collection.AddScoped<IOrganisationRepository>(sp => sp.GetRequiredService<FakeOrganisationRepository>());
                collection.AddScoped<IOrganisationMembershipRepository>(sp => sp.GetRequiredService<FakeOrganisationMembershipRepository>());
                collection.AddScoped<IOrganisationInvitationRepository>(sp => sp.GetRequiredService<FakeOrganisationInvitationRepository>());
                collection.AddScoped<ITaxonomyLookupRepository>(sp => sp.GetRequiredService<FakeTaxonomyLookupRepository>());
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

    private sealed class FakeTaxonomyLookupRepository : ITaxonomyLookupRepository
    {
        private readonly Dictionary<long, JobFamily> _jobFamilies = new();
        private readonly Dictionary<long, Role> _roles = new();
        private readonly Dictionary<long, Specialisation> _specialisations = new();
        private readonly Dictionary<long, WorkContext> _workContexts = new();
        private readonly Dictionary<long, Skill> _skills = new();
        private readonly Dictionary<long, TaxonomyVersion> _taxonomyVersions = new();
        private readonly List<Geography> _geographies = new();

        public FakeTaxonomyLookupRepository()
        {
            SeedTestData();
        }

        private void SeedTestData()
        {
            var taxonomyVersion = TaxonomyVersion.Create("1.0.0", DateTime.UtcNow, TaxonomyVersionStatus.Active, "Initial release");
            _taxonomyVersions[taxonomyVersion.Id] = taxonomyVersion;

            var jobFamily = JobFamily.Create("DELIVERY", "Delivery", taxonomyVersion.Id, null, true);
            SetPrivateProperty(jobFamily, nameof(JobFamily.TaxonomyVersion), taxonomyVersion);
            _jobFamilies[jobFamily.Id] = jobFamily;

            var role = Role.Create("Delivery Driver", "delivery-driver", jobFamily.Id, taxonomyVersion.Id, true);
            SetPrivateProperty(role, nameof(Role.JobFamily), jobFamily);
            SetPrivateProperty(role, nameof(Role.TaxonomyVersion), taxonomyVersion);
            _roles[role.Id] = role;

            var specialisation = Specialisation.Create(role.Id, "Last Mile Specialist", "LAST_MILE", null, true);
            SetPrivateProperty(specialisation, nameof(Specialisation.Role), role);
            _specialisations[specialisation.Id] = specialisation;

            var workContext = WorkContext.Create(role.Id, "SHIFT_DAY", "Day Shift", true);
            SetPrivateProperty(workContext, nameof(WorkContext.Role), role);
            _workContexts[workContext.Id] = workContext;

            var skill = Skill.Create("DRV_SAFE", "Safe Driving", taxonomyVersion.Id, null, "Basic safety certification", true);
            SetPrivateProperty(skill, nameof(Skill.TaxonomyVersion), taxonomyVersion);
            _skills[skill.Id] = skill;
        }

        private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value)
        {
            var property = typeof(TTarget).GetProperty(
                propertyName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            property?.SetValue(target, value);
        }

        public Task<IReadOnlyList<JobFamily>> GetJobFamiliesAsync(
            long? taxonomyVersionId = null,
            string? search = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var result = _jobFamilies.Values
                .Where(f => !taxonomyVersionId.HasValue || f.TaxonomyVersionId == taxonomyVersionId.Value)
                .Where(f => !isActive.HasValue || f.IsActive == isActive.Value)
                .Where(f => string.IsNullOrWhiteSpace(search) ||
                    (f.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     f.Code.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Task.FromResult<IReadOnlyList<JobFamily>>(result);
        }

        public Task<IReadOnlyList<Role>> GetRolesAsync(
            long? jobFamilyId = null,
            long? taxonomyVersionId = null,
            string? search = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var result = _roles.Values
                .Where(r => !jobFamilyId.HasValue || r.JobFamilyId == jobFamilyId.Value)
                .Where(r => !taxonomyVersionId.HasValue || r.TaxonomyVersionId == taxonomyVersionId.Value)
                .Where(r => !isActive.HasValue || r.IsActive == isActive.Value)
                .Where(r => string.IsNullOrWhiteSpace(search) ||
                    (r.CanonicalName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     r.Slug.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Task.FromResult<IReadOnlyList<Role>>(result);
        }

        public Task<IReadOnlyList<Specialisation>> GetSpecialisationsAsync(
            long? roleId = null,
            string? search = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var result = _specialisations.Values
                .Where(s => !roleId.HasValue || s.RoleId == roleId.Value)
                .Where(s => !isActive.HasValue || s.IsActive == isActive.Value)
                .Where(s => string.IsNullOrWhiteSpace(search) ||
                    (s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     s.Code.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Task.FromResult<IReadOnlyList<Specialisation>>(result);
        }

        public Task<IReadOnlyList<WorkContext>> GetWorkContextsAsync(
            long? roleId = null,
            string? search = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var result = _workContexts.Values
                .Where(w => !roleId.HasValue || w.RoleId == roleId.Value)
                .Where(w => !isActive.HasValue || w.IsActive == isActive.Value)
                .Where(w => string.IsNullOrWhiteSpace(search) ||
                    (w.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     w.Code.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Task.FromResult<IReadOnlyList<WorkContext>>(result);
        }

        public Task<IReadOnlyList<Skill>> GetSkillsAsync(
            long? taxonomyVersionId = null,
            string? search = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var result = _skills.Values
                .Where(s => !taxonomyVersionId.HasValue || s.TaxonomyVersionId == taxonomyVersionId.Value)
                .Where(s => !isActive.HasValue || s.IsActive == isActive.Value)
                .Where(s => string.IsNullOrWhiteSpace(search) ||
                    (s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                     s.Code.Contains(search, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Task.FromResult<IReadOnlyList<Skill>>(result);
        }

        public Task<IReadOnlyList<TaxonomyVersion>> GetTaxonomyVersionsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TaxonomyVersion>>(_taxonomyVersions.Values.ToList());
        }

        public Task<IReadOnlyList<Geography>> GetGeographiesAsync(
            GeographyType? type = null,
            string? search = null,
            bool? hasCoordinates = null,
            CancellationToken cancellationToken = default)
        {
            var result = _geographies
                .Where(g => !type.HasValue || g.Type == type.Value)
                .Where(g => string.IsNullOrWhiteSpace(search) ||
                    g.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (g.Code?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
                .Where(g => !hasCoordinates.HasValue ||
                    (hasCoordinates.Value
                        ? g.Latitude.HasValue && g.Longitude.HasValue
                        : !g.Latitude.HasValue || !g.Longitude.HasValue))
                .ToList();

            return Task.FromResult<IReadOnlyList<Geography>>(result);
        }
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

    // ── JobFamilies query ─────────────────────────────────────────────────

    [Fact]
    public async Task JobFamilies_happy_path_returns_job_families()
    {
        // Act
        var query = new { query = @"{ jobFamilies { id code name taxonomyVersion isActive } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("jobFamilies");
        Assert.Equal(1, data.GetArrayLength());
        var family = data[0];
        Assert.Equal("DELIVERY", family.GetProperty("code").GetString());
        Assert.Equal("Delivery", family.GetProperty("name").GetString());
        Assert.Equal("1.0.0", family.GetProperty("taxonomyVersion").GetString());
        Assert.True(family.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task JobFamilies_with_search_filters_results()
    {
        // Act
        var query = new { query = @"{ jobFamilies(search: ""NonExistent"") { id code name } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("jobFamilies");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task JobFamilies_inactive_only_returns_empty()
    {
        // Act
        var query = new { query = @"{ jobFamilies(isActive: false) { id code name } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("jobFamilies");
        Assert.Equal(0, data.GetArrayLength());
    }

    // ── Roles query ───────────────────────────────────────────────────────

    [Fact]
    public async Task Roles_happy_path_returns_roles()
    {
        // Act
        var query = new { query = @"{ roles { id canonicalName slug jobFamilyCode taxonomyVersion } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("roles");
        Assert.Equal(1, data.GetArrayLength());
        var role = data[0];
        Assert.Equal("Delivery Driver", role.GetProperty("canonicalName").GetString());
        Assert.Equal("delivery-driver", role.GetProperty("slug").GetString());
        Assert.Equal("DELIVERY", role.GetProperty("jobFamilyCode").GetString());
    }

    [Fact]
    public async Task Roles_with_search_returns_matching_role()
    {
        // Act
        var query = new { query = @"{ roles(search: ""driver"") { id canonicalName slug } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("roles");
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal("Delivery Driver", data[0].GetProperty("canonicalName").GetString());
    }

    // ── Specialisations query ─────────────────────────────────────────────

    [Fact]
    public async Task Specialisations_happy_path_returns_specialisations()
    {
        // Act
        var query = new { query = @"{ specialisations { id name code roleCanonicalName } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("specialisations");
        Assert.Equal(1, data.GetArrayLength());
        var spec = data[0];
        Assert.Equal("Last Mile Specialist", spec.GetProperty("name").GetString());
        Assert.Equal("LAST_MILE", spec.GetProperty("code").GetString());
        Assert.Equal("Delivery Driver", spec.GetProperty("roleCanonicalName").GetString());
    }

    [Fact]
    public async Task Specialisations_inactive_only_returns_empty()
    {
        // Act
        var query = new { query = @"{ specialisations(isActive: false) { id name } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("specialisations");
        Assert.Equal(0, data.GetArrayLength());
    }

    // ── WorkContexts query ────────────────────────────────────────────────

    [Fact]
    public async Task WorkContexts_happy_path_returns_work_contexts()
    {
        // Act
        var query = new { query = @"{ workContexts { id code name roleCanonicalName } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("workContexts");
        Assert.Equal(1, data.GetArrayLength());
        var ctx = data[0];
        Assert.Equal("SHIFT_DAY", ctx.GetProperty("code").GetString());
        Assert.Equal("Day Shift", ctx.GetProperty("name").GetString());
    }

    // ── Skills query ──────────────────────────────────────────────────────

    [Fact]
    public async Task Skills_happy_path_returns_skills()
    {
        // Act
        var query = new { query = @"{ skills { id code name description taxonomyVersion } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("skills");
        Assert.Equal(1, data.GetArrayLength());
        var skill = data[0];
        Assert.Equal("DRV_SAFE", skill.GetProperty("code").GetString());
        Assert.Equal("Safe Driving", skill.GetProperty("name").GetString());
        Assert.Equal("1.0.0", skill.GetProperty("taxonomyVersion").GetString());
    }

    // ── TaxonomyVersions query ────────────────────────────────────────────

    [Fact]
    public async Task TaxonomyVersions_happy_path_returns_versions()
    {
        // Act
        var query = new { query = @"{ taxonomyVersions { id semanticVersion status } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("taxonomyVersions");
        Assert.Equal(1, data.GetArrayLength());
        var version = data[0];
        Assert.Equal("1.0.0", version.GetProperty("semanticVersion").GetString());
        Assert.Equal("Active", version.GetProperty("status").GetString());
    }

    // ── Geographies query ─────────────────────────────────────────────────

    [Fact]
    public async Task Geographies_returns_empty_when_no_seed_data()
    {
        // Act
        var query = new { query = @"{ geographies { id name type code } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("geographies");
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task GeographyHierarchy_returns_empty_when_no_seed_data()
    {
        // Act
        var query = new { query = @"{ geographyHierarchy { id name parentName } }" };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Null(GetErrors(content));
        var data = GetData(content).GetProperty("geographyHierarchy");
        Assert.Equal(0, data.GetArrayLength());
    }
}
