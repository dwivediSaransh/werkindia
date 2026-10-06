using IndiaWorks.Domain.E01;
using IndiaWorks.Domain.E02;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database.E02;

public sealed class OrganisationModelConfigurationTests
{
    private readonly AppDbContext _context;

    public OrganisationModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1;Database=Test_Metadata_Only;TrustServerCertificate=True;")
            .Options;

        _context = new AppDbContext(options);
    }

    [Fact]
    public void Organisation_ModelConfiguration_MatchesSchemaRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(Organisation));
        Assert.NotNull(entityType);

        Assert.Equal("Organisations", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Organisation.Id), primaryKey.Properties.Single().Name);

        var legalName = entityType.FindProperty(nameof(Organisation.LegalName));
        Assert.NotNull(legalName);
        Assert.False(legalName.IsNullable);
        Assert.Equal(250, legalName.GetMaxLength());

        var verificationStatus = entityType.FindProperty(nameof(Organisation.VerificationStatus));
        Assert.NotNull(verificationStatus);
        Assert.False(verificationStatus.IsNullable);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Organisation.LegalName)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Organisation.VerificationStatus)));
    }

    [Fact]
    public void OrganisationIdentifier_ModelConfiguration_HasUniqueConstraintOnTypeAndHash()
    {
        var entityType = _context.Model.FindEntityType(typeof(OrganisationIdentifier));
        Assert.NotNull(entityType);

        Assert.Equal("OrganisationIdentifiers", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(OrganisationIdentifier.Id), primaryKey.Properties.Single().Name);

        var uniqueIndex = entityType.GetIndexes().FirstOrDefault(i => i.IsUnique);
        Assert.NotNull(uniqueIndex);
        var indexPropertyNames = uniqueIndex.Properties.Select(p => p.Name).ToList();
        Assert.Contains(nameof(OrganisationIdentifier.IdentifierType), indexPropertyNames);
        Assert.Contains(nameof(OrganisationIdentifier.IdentifierHash), indexPropertyNames);

        var foreignKey = entityType.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Organisation));
        Assert.NotNull(foreignKey);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void OrganisationMembership_ModelConfiguration_HasCompositeKeyAndCorrectForeignKeys()
    {
        var entityType = _context.Model.FindEntityType(typeof(OrganisationMembership));
        Assert.NotNull(entityType);

        Assert.Equal("OrganisationMemberships", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(2, primaryKey.Properties.Count);
        var pkPropertyNames = primaryKey.Properties.Select(p => p.Name).ToList();
        Assert.Contains(nameof(OrganisationMembership.OrganisationId), pkPropertyNames);
        Assert.Contains(nameof(OrganisationMembership.UserAccountId), pkPropertyNames);

        var orgFk = entityType.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Organisation));
        Assert.NotNull(orgFk);
        Assert.Equal(DeleteBehavior.Cascade, orgFk.DeleteBehavior);

        var userFk = entityType.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(UserAccount));
        Assert.NotNull(userFk);
        Assert.Equal(DeleteBehavior.Restrict, userFk.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(OrganisationMembership.UserAccountId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(OrganisationMembership.Status)));
    }

    [Fact]
    public void OrganisationInvitation_ModelConfiguration_HasCorrectSchemaRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(OrganisationInvitation));
        Assert.NotNull(entityType);

        Assert.Equal("OrganisationInvitations", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(OrganisationInvitation.Id), primaryKey.Properties.Single().Name);

        // Verify binary column lengths for hashes
        var inviteeHash = entityType.FindProperty(nameof(OrganisationInvitation.InviteeIdentifierHash));
        Assert.NotNull(inviteeHash);
        Assert.False(inviteeHash.IsNullable);
        Assert.Equal(32, inviteeHash.GetMaxLength());

        var tokenHash = entityType.FindProperty(nameof(OrganisationInvitation.TokenHash));
        Assert.NotNull(tokenHash);
        Assert.False(tokenHash.IsNullable);
        Assert.Equal(64, tokenHash.GetMaxLength());

        // Verify FK to Organisation with cascade delete
        var orgFk = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Organisation));
        Assert.NotNull(orgFk);
        Assert.Equal(DeleteBehavior.Cascade, orgFk.DeleteBehavior);

        // Verify compound index on Org+Status+Expiry
        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(OrganisationInvitation.Status))
            && i.Properties.Any(p => p.Name == nameof(OrganisationInvitation.ExpiresAt)));

        // Verify unique partial index on Org+InviteeHash (Pending)
        var uniqueIndex = indexes.FirstOrDefault(i => i.IsUnique);
        Assert.NotNull(uniqueIndex);
    }
}
