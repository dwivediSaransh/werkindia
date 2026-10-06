using System.Security.Cryptography;
using System.Text;
using IndiaWorks.Domain.E02;
using Xunit;

namespace IndiaWorks.Domain.Tests.E02;

public sealed class OrganisationTests
{
    [Fact]
    public void Create_WithValidParameters_SetsPropertiesCorrectly()
    {
        var org = Organisation.Create(
            legalName: "Acme Technologies Private Limited",
            tradeName: "Acme Tech",
            sector: EmployerSector.Private,
            employeeBand: "51-200",
            organisationType: "Private Limited",
            nic2025Code: "6201");

        Assert.NotEqual(Guid.Empty, org.Id);
        Assert.Equal("Acme Technologies Private Limited", org.LegalName);
        Assert.Equal("Acme Tech", org.TradeName);
        Assert.Equal(EmployerSector.Private, org.Sector);
        Assert.Equal("51-200", org.EmployeeBand);
        Assert.Equal("Private Limited", org.OrganisationType);
        Assert.Equal("6201", org.Nic2025Code);
        Assert.Equal(VerificationStatus.Unverified, org.VerificationStatus);
        Assert.True(org.CreatedAt <= DateTime.UtcNow);
        Assert.Empty(org.Identifiers);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidLegalName_ThrowsArgumentException(string? invalidName)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            Organisation.Create(legalName: invalidName!));
    }

    [Fact]
    public void UpdateProfile_UpdatesFieldsCorrectly()
    {
        var org = Organisation.Create("Initial Name");
        org.UpdateProfile(
            legalName: "Updated Legal Name",
            tradeName: "Updated Trade Name",
            sector: EmployerSector.Psu,
            employeeBand: "501-1000",
            organisationType: "Public Sector",
            nic2025Code: "8411");

        Assert.Equal("Updated Legal Name", org.LegalName);
        Assert.Equal("Updated Trade Name", org.TradeName);
        Assert.Equal(EmployerSector.Psu, org.Sector);
        Assert.Equal("501-1000", org.EmployeeBand);
        Assert.Equal("Public Sector", org.OrganisationType);
        Assert.Equal("8411", org.Nic2025Code);
    }

    [Fact]
    public void AddIdentifier_AddsIdentifierSuccessfully()
    {
        var org = Organisation.Create("Bharat Enterprises");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("AAACB1234K"));

        var identifier = org.AddIdentifier(
            IdentifierType.Pan,
            hash,
            maskedDisplay: "AA***1234K");

        Assert.NotNull(identifier);
        Assert.Equal(org.Id, identifier.OrganisationId);
        Assert.Equal(IdentifierType.Pan, identifier.IdentifierType);
        Assert.Equal(hash, identifier.IdentifierHash);
        Assert.Equal("AA***1234K", identifier.MaskedDisplay);
        Assert.Equal(VerificationStatus.Unverified, identifier.VerificationStatus);
        Assert.Single(org.Identifiers);
    }

    [Fact]
    public void AddIdentifier_DuplicateHash_ThrowsInvalidOperationException()
    {
        var org = Organisation.Create("Bharat Enterprises");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("AAACB1234K"));

        org.AddIdentifier(IdentifierType.Pan, hash, "AA***1234K");

        Assert.Throws<InvalidOperationException>(() =>
            org.AddIdentifier(IdentifierType.Pan, hash, "AA***1234K"));
    }

    [Fact]
    public void MarkVerified_UpdatesVerificationStatusAndSource()
    {
        var org = Organisation.Create("Bharat Enterprises");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("AAACB1234K"));
        var id = org.AddIdentifier(IdentifierType.Pan, hash);

        id.MarkVerified("NSDL API", DateTime.UtcNow);

        Assert.Equal(VerificationStatus.Verified, id.VerificationStatus);
        Assert.Equal("NSDL API", id.VerificationSource);
        Assert.NotNull(id.VerifiedAt);
    }

    [Fact]
    public void MarkRejected_UpdatesVerificationStatusAndReason()
    {
        var org = Organisation.Create("Bharat Enterprises");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("AAACB1234K"));
        var id = org.AddIdentifier(IdentifierType.Pan, hash);

        id.MarkRejected("Invalid document format");

        Assert.Equal(VerificationStatus.Rejected, id.VerificationStatus);
        Assert.Equal("Invalid document format", id.VerificationSource);
    }
}
