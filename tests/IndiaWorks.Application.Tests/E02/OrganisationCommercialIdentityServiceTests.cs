using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Domain.E02;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IndiaWorks.Application.Tests.E02;

public sealed class OrganisationCommercialIdentityServiceTests
{
    private readonly InMemoryOrganisationRepository _orgRepo = new();
    private readonly OrganisationCommercialIdentityService _service;

    public OrganisationCommercialIdentityServiceTests()
    {
        _service = new OrganisationCommercialIdentityService(
            _orgRepo,
            NullLogger<OrganisationCommercialIdentityService>.Instance);
    }

    [Fact]
    public async Task RecomputeFingerprintAsync_EmptyOrganisationId_ReturnsValidationFailed()
    {
        var result = await _service.RecomputeFingerprintAsync(Guid.Empty);
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Contains("OrganisationId", result.ErrorMessage);
    }

    [Fact]
    public async Task RecomputeFingerprintAsync_NonExistentOrganisation_ReturnsOrganisationNotFound()
    {
        var result = await _service.RecomputeFingerprintAsync(Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Equal("ORGANISATION_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task RecomputeFingerprintAsync_OrganisationWithNoVerifiedIdentifiers_ClearsFingerprint()
    {
        var org = Organisation.Create("Test Org", "Test", EmployerSector.Private);
        await _orgRepo.AddAsync(org);
        await _orgRepo.SaveChangesAsync();

        var result = await _service.RecomputeFingerprintAsync(org.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(org.Id, result.OrganisationId);
        Assert.Null(result.Fingerprint);
        var persisted = await _orgRepo.GetByIdAsync(org.Id);
        Assert.Null(persisted?.CommercialIdentityFingerprint);
    }

    [Fact]
    public async Task RecomputeFingerprintAsync_WithOneVerifiedIdentifier_SetsFingerprint()
    {
        var org = Organisation.Create("Test Org", "Test", EmployerSector.Private);
        var identifier = org.AddIdentifier(
            IdentifierType.Pan,
            RandomHasher.Compute("ABCD12345678"),
            maskedDisplay: "ABCD****5678");
        identifier.MarkVerified("PAN verification service", DateTime.UtcNow);

        await _orgRepo.AddAsync(org);
        await _orgRepo.SaveChangesAsync();

        var result = await _service.RecomputeFingerprintAsync(org.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Fingerprint);
        Assert.Equal(32, result.Fingerprint.Length);
        var persisted = await _orgRepo.GetByIdAsync(org.Id);
        Assert.NotNull(persisted?.CommercialIdentityFingerprint);
        Assert.True(persisted!.CommercialIdentityFingerprint!.SequenceEqual(result.Fingerprint));
    }

    [Fact]
    public async Task RecomputeFingerprintAsync_WithMultipleVerifiedIdentifiers_SetsDeterministicFingerprint()
    {
        var org = Organisation.Create("Test Org", "Test", EmployerSector.Private);

        var pan = org.AddIdentifier(IdentifierType.Pan, RandomHasher.Compute("PAN001"), maskedDisplay: "PAN001");
        pan.MarkVerified("PAN service");

        var gstin = org.AddIdentifier(IdentifierType.Gstin, RandomHasher.Compute("GST001"), maskedDisplay: "GST001");
        gstin.MarkVerified("GST service");

        var cin = org.AddIdentifier(IdentifierType.Cin, RandomHasher.Compute("CIN001"), maskedDisplay: "CIN001");
        cin.MarkVerified("CIN service");

        await _orgRepo.AddAsync(org);
        await _orgRepo.SaveChangesAsync();

        // Compute expected fingerprint manually using the same algorithm
        // IdentifierType enum order: Pan(0), Gstin(1), Cin(2), Llpin(3)
        var orderedHashes = new[] { pan.IdentifierHash, gstin.IdentifierHash, cin.IdentifierHash };

        // Compute using the actual algorithm: concatenate in IdentifierType order
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var ms = new System.IO.MemoryStream();
        foreach (var hash in orderedHashes)
        {
            ms.Write(hash);
        }
        var expectedFingerprint = sha.ComputeHash(ms.ToArray());

        var result = await _service.RecomputeFingerprintAsync(org.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Fingerprint);
        Assert.True(result.Fingerprint!.SequenceEqual(expectedFingerprint));
    }

    [Fact]
    public async Task RecomputeFingerprintAsync_OnlyVerifiedIdentifiersContribute_UnverifiedIgnored()
    {
        var org = Organisation.Create("Test Org", "Test", EmployerSector.Private);

        var panVerified = org.AddIdentifier(IdentifierType.Pan, RandomHasher.Compute("VERIFIED_PAN"), maskedDisplay: "VPAN");
        panVerified.MarkVerified("PAN service");

        var gstinUnverified = org.AddIdentifier(IdentifierType.Gstin, RandomHasher.Compute("UNVERIFIED_GST"), maskedDisplay: "UGST");
        // Unverified - not marked

        await _orgRepo.AddAsync(org);
        await _orgRepo.SaveChangesAsync();

        var result = await _service.RecomputeFingerprintAsync(org.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Fingerprint);

        // Now verify the fingerprint only contains the verified hash
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var ms = new System.IO.MemoryStream();
        ms.Write(panVerified.IdentifierHash);
        var expectedFingerprint = sha.ComputeHash(ms.ToArray());

        Assert.True(result.Fingerprint!.SequenceEqual(expectedFingerprint));
    }

    [Fact]
    public async Task GetFingerprintAsync_EmptyOrganisationId_ReturnsValidationFailed()
    {
        var result = await _service.GetFingerprintAsync(Guid.Empty);
        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
    }

    [Fact]
    public async Task GetFingerprintAsync_NonExistentOrganisation_ReturnsOrganisationNotFound()
    {
        var result = await _service.GetFingerprintAsync(Guid.NewGuid());
        Assert.False(result.IsSuccess);
        Assert.Equal("ORGANISATION_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task GetFingerprintAsync_ReturnsExistingFingerprint()
    {
        var org = Organisation.Create("Test Org", "Test", EmployerSector.Private);
        var identifier = org.AddIdentifier(IdentifierType.Pan, RandomHasher.Compute("SOME_PAN"), maskedDisplay: "SPAN");
        identifier.MarkVerified("PAN verification");

        await _orgRepo.AddAsync(org);
        await _orgRepo.SaveChangesAsync();

        await _service.RecomputeFingerprintAsync(org.Id);

        var result = await _service.GetFingerprintAsync(org.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Fingerprint);
        Assert.Equal(32, result.Fingerprint.Length);
    }

    [Fact]
    public async Task RecomputeFingerprintAsync_AfterNewIdentifierVerified_UpdatesFingerprint()
    {
        var org = Organisation.Create("Test Org", "Test", EmployerSector.Private);
        var pan = org.AddIdentifier(IdentifierType.Pan, RandomHasher.Compute("PAN_ONLY"), maskedDisplay: "PAN");
        pan.MarkVerified("PAN service");

        await _orgRepo.AddAsync(org);
        await _orgRepo.SaveChangesAsync();

        var firstResult = await _service.RecomputeFingerprintAsync(org.Id);
        Assert.NotNull(firstResult.Fingerprint);

        var gstin = org.AddIdentifier(IdentifierType.Gstin, RandomHasher.Compute("GST_ADDED"), maskedDisplay: "GST");
        gstin.MarkVerified("GST service");

        await _orgRepo.SaveChangesAsync();

        var secondResult = await _service.RecomputeFingerprintAsync(org.Id);
        Assert.NotNull(secondResult.Fingerprint);
        Assert.NotEqual(firstResult.Fingerprint, secondResult.Fingerprint);

        // Verify ordering is by IdentifierType enum value, not insertion order
        // Cin=3, Gstin=1, Pan=0 → order: Pan(0), Gstin(1), Cin(3)
        // Wait, IdentifierType enum: Pan=0, Gstin=1, Cin=2, Llpin=3
        // So order by enum: Pan, Gstin, Cin, Llpin → Pan(0), Gstin(1)
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var ms = new System.IO.MemoryStream();
        ms.Write(pan.IdentifierHash);
        ms.Write(gstin.IdentifierHash);
        var expectedFingerprint = sha.ComputeHash(ms.ToArray());

        Assert.True(secondResult.Fingerprint!.SequenceEqual(expectedFingerprint));
    }

    // ── In-memory fakes ───────────────────────────────────────────────────────

    private sealed class InMemoryOrganisationRepository : IOrganisationRepository
    {
        private readonly Dictionary<Guid, Organisation> _organisations = new();
        public int SaveChangesCount { get; private set; }
        public IReadOnlyCollection<Organisation> Organisations => _organisations.Values.ToList();

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
            var orgs = ids.Where(_organisations.ContainsKey).Select(id => _organisations[id]).ToList();
            return Task.FromResult<IReadOnlyList<Organisation>>(orgs);
        }

        public Task<IReadOnlyList<Organisation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Organisation>>(_organisations.Values.ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }

    private static class RandomHasher
    {
        public static byte[] Compute(string value)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
        }
    }
}
