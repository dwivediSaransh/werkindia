using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndiaWorks.IntegrationTests.Database.E03;

public sealed class TaxonomyModelConfigurationTests
{
    private readonly AppDbContext _context;

    public TaxonomyModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1;Database=Test_Metadata_Only;TrustServerCertificate=True;")
            .Options;

        _context = new AppDbContext(options);
    }

    [Fact]
    public void TaxonomyVersion_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(TaxonomyVersion));
        Assert.NotNull(entityType);

        Assert.Equal("TaxonomyVersions", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(TaxonomyVersion.Id), primaryKey.Properties.Single().Name);

        var semanticVersion = entityType.FindProperty(nameof(TaxonomyVersion.SemanticVersion));
        Assert.NotNull(semanticVersion);
        Assert.False(semanticVersion.IsNullable);
        Assert.Equal(30, semanticVersion.GetMaxLength());

        var effectiveFrom = entityType.FindProperty(nameof(TaxonomyVersion.EffectiveFrom));
        Assert.NotNull(effectiveFrom);
        Assert.False(effectiveFrom.IsNullable);

        var status = entityType.FindProperty(nameof(TaxonomyVersion.Status));
        Assert.NotNull(status);
        Assert.False(status.IsNullable);
        Assert.Equal(20, status.GetMaxLength());

        var releaseNotes = entityType.FindProperty(nameof(TaxonomyVersion.ReleaseNotes));
        Assert.NotNull(releaseNotes);
        Assert.True(releaseNotes.IsNullable);

        var createdAt = entityType.FindProperty(nameof(TaxonomyVersion.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(TaxonomyVersion.SemanticVersion)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(TaxonomyVersion.Status)));
    }

    [Fact]
    public void TaxonomyVersions_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.TaxonomyVersions));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<TaxonomyVersion>), dbSetProperty.PropertyType);
    }

    [Fact]
    public void JobFamily_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(JobFamily));
        Assert.NotNull(entityType);

        Assert.Equal("JobFamilies", entityType.GetTableName());

        var id = entityType.FindProperty(nameof(JobFamily.Id));
        Assert.NotNull(id);
        Assert.False(id.IsNullable);

        var code = entityType.FindProperty(nameof(JobFamily.Code));
        Assert.NotNull(code);
        Assert.False(code.IsNullable);
        Assert.Equal(50, code.GetMaxLength());

        var name = entityType.FindProperty(nameof(JobFamily.Name));
        Assert.NotNull(name);
        Assert.False(name.IsNullable);
        Assert.Equal(200, name.GetMaxLength());

        var parentId = entityType.FindProperty(nameof(JobFamily.ParentId));
        Assert.NotNull(parentId);
        Assert.True(parentId.IsNullable);

        var taxonomyVersionId = entityType.FindProperty(nameof(JobFamily.TaxonomyVersionId));
        Assert.NotNull(taxonomyVersionId);
        Assert.False(taxonomyVersionId.IsNullable);

        var isActive = entityType.FindProperty(nameof(JobFamily.IsActive));
        Assert.NotNull(isActive);
        Assert.False(isActive.IsNullable);

        var createdAt = entityType.FindProperty(nameof(JobFamily.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var parentFk = foreignKeys.FirstOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(JobFamily));
        Assert.NotNull(parentFk);
        Assert.Equal(DeleteBehavior.Restrict, parentFk!.DeleteBehavior);

        var taxVersionFk = foreignKeys.FirstOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(TaxonomyVersion));
        Assert.NotNull(taxVersionFk);
        Assert.Equal(DeleteBehavior.Cascade, taxVersionFk!.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique
            && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(JobFamily.TaxonomyVersionId))
            && i.Properties.Any(p => p.Name == nameof(JobFamily.Code)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(JobFamily.ParentId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(JobFamily.TaxonomyVersionId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(JobFamily.Name)));
    }

    [Fact]
    public void JobFamilies_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.JobFamilies));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<JobFamily>), dbSetProperty.PropertyType);
    }

    [Fact]
    public void Role_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(Role));
        Assert.NotNull(entityType);

        Assert.Equal("Roles", entityType.GetTableName());

        var id = entityType.FindProperty(nameof(Role.Id));
        Assert.NotNull(id);
        Assert.False(id.IsNullable);

        var canonicalName = entityType.FindProperty(nameof(Role.CanonicalName));
        Assert.NotNull(canonicalName);
        Assert.False(canonicalName.IsNullable);
        Assert.Equal(200, canonicalName.GetMaxLength());

        var slug = entityType.FindProperty(nameof(Role.Slug));
        Assert.NotNull(slug);
        Assert.False(slug.IsNullable);
        Assert.Equal(200, slug.GetMaxLength());

        var jobFamilyId = entityType.FindProperty(nameof(Role.JobFamilyId));
        Assert.NotNull(jobFamilyId);
        Assert.False(jobFamilyId.IsNullable);

        var taxonomyVersionId = entityType.FindProperty(nameof(Role.TaxonomyVersionId));
        Assert.NotNull(taxonomyVersionId);
        Assert.False(taxonomyVersionId.IsNullable);

        var isActive = entityType.FindProperty(nameof(Role.IsActive));
        Assert.NotNull(isActive);
        Assert.False(isActive.IsNullable);

        var validFrom = entityType.FindProperty(nameof(Role.ValidFrom));
        Assert.NotNull(validFrom);
        Assert.False(validFrom.IsNullable);

        var validTo = entityType.FindProperty(nameof(Role.ValidTo));
        Assert.NotNull(validTo);
        Assert.True(validTo.IsNullable);

        var createdAt = entityType.FindProperty(nameof(Role.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var jobFamilyFk = foreignKeys.FirstOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(JobFamily));
        Assert.NotNull(jobFamilyFk);
        Assert.Equal(DeleteBehavior.Restrict, jobFamilyFk!.DeleteBehavior);

        var taxVersionFk = foreignKeys.FirstOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(TaxonomyVersion));
        Assert.NotNull(taxVersionFk);
        Assert.Equal(DeleteBehavior.Cascade, taxVersionFk!.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique
            && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(Role.TaxonomyVersionId))
            && i.Properties.Any(p => p.Name == nameof(Role.Slug)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Role.JobFamilyId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Role.TaxonomyVersionId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Role.CanonicalName)));
    }

    [Fact]
    public void Roles_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.Roles));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<Role>), dbSetProperty.PropertyType);
    }

    [Fact]
    public void RoleAlias_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(RoleAlias));
        Assert.NotNull(entityType);

        Assert.Equal("RoleAliases", entityType.GetTableName());

        var id = entityType.FindProperty(nameof(RoleAlias.Id));
        Assert.NotNull(id);
        Assert.False(id.IsNullable);

        var roleId = entityType.FindProperty(nameof(RoleAlias.RoleId));
        Assert.NotNull(roleId);
        Assert.False(roleId.IsNullable);

        var alias = entityType.FindProperty(nameof(RoleAlias.Alias));
        Assert.NotNull(alias);
        Assert.False(alias.IsNullable);
        Assert.Equal(250, alias.GetMaxLength());

        var languageCode = entityType.FindProperty(nameof(RoleAlias.LanguageCode));
        Assert.NotNull(languageCode);
        Assert.True(languageCode.IsNullable);
        Assert.Equal(10, languageCode.GetMaxLength());

        var regionCode = entityType.FindProperty(nameof(RoleAlias.RegionCode));
        Assert.NotNull(regionCode);
        Assert.True(regionCode.IsNullable);
        Assert.Equal(50, regionCode.GetMaxLength());

        var aliasType = entityType.FindProperty(nameof(RoleAlias.AliasType));
        Assert.NotNull(aliasType);
        Assert.True(aliasType.IsNullable);
        Assert.Equal(30, aliasType.GetMaxLength());

        var weight = entityType.FindProperty(nameof(RoleAlias.Weight));
        Assert.NotNull(weight);
        Assert.True(weight.IsNullable);
        Assert.Equal(6, weight.GetPrecision());
        Assert.Equal(3, weight.GetScale());

        var isDeprecated = entityType.FindProperty(nameof(RoleAlias.IsDeprecated));
        Assert.NotNull(isDeprecated);
        Assert.False(isDeprecated.IsNullable);

        var createdAt = entityType.FindProperty(nameof(RoleAlias.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var roleFk = foreignKeys.First(fk =>
            fk.PrincipalEntityType.ClrType == typeof(Role));
        Assert.Equal(DeleteBehavior.Cascade, roleFk.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique
            && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(RoleAlias.RoleId))
            && i.Properties.Any(p => p.Name == nameof(RoleAlias.Alias)));
        Assert.Contains(indexes, i =>
            !i.IsUnique
            && i.Properties.Any(p => p.Name == nameof(RoleAlias.Alias)));
    }

    [Fact]
    public void RoleAliases_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.RoleAliases));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<RoleAlias>), dbSetProperty.PropertyType);
    }

    [Fact]
    public void Specialisation_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(Specialisation));
        Assert.NotNull(entityType);

        Assert.Equal("Specialisations", entityType.GetTableName());

        var id = entityType.FindProperty(nameof(Specialisation.Id));
        Assert.NotNull(id);
        Assert.False(id.IsNullable);

        var roleId = entityType.FindProperty(nameof(Specialisation.RoleId));
        Assert.NotNull(roleId);
        Assert.False(roleId.IsNullable);

        var name = entityType.FindProperty(nameof(Specialisation.Name));
        Assert.NotNull(name);
        Assert.False(name.IsNullable);
        Assert.Equal(200, name.GetMaxLength());

        var code = entityType.FindProperty(nameof(Specialisation.Code));
        Assert.NotNull(code);
        Assert.False(code.IsNullable);
        Assert.Equal(80, code.GetMaxLength());

        var parentId = entityType.FindProperty(nameof(Specialisation.ParentId));
        Assert.NotNull(parentId);
        Assert.True(parentId.IsNullable);

        var isActive = entityType.FindProperty(nameof(Specialisation.IsActive));
        Assert.NotNull(isActive);
        Assert.False(isActive.IsNullable);

        var createdAt = entityType.FindProperty(nameof(Specialisation.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var foreignKeys = entityType.GetForeignKeys().ToList();

        var roleFk = foreignKeys.FirstOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(Role));
        Assert.NotNull(roleFk);
        Assert.Equal(DeleteBehavior.Cascade, roleFk!.DeleteBehavior);

        var parentFk = foreignKeys.FirstOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(Specialisation));
        Assert.NotNull(parentFk);
        Assert.Equal(DeleteBehavior.Restrict, parentFk!.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique
            && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(Specialisation.RoleId))
            && i.Properties.Any(p => p.Name == nameof(Specialisation.Code)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Specialisation.ParentId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Specialisation.RoleId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Specialisation.Name)));
    }

    [Fact]
    public void Specialisations_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.Specialisations));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<Specialisation>), dbSetProperty.PropertyType);
    }

    [Fact]
    public void WorkContext_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(WorkContext));
        Assert.NotNull(entityType);

        Assert.Equal("WorkContexts", entityType.GetTableName());

        var id = entityType.FindProperty(nameof(WorkContext.Id));
        Assert.NotNull(id);
        Assert.False(id.IsNullable);

        var roleId = entityType.FindProperty(nameof(WorkContext.RoleId));
        Assert.NotNull(roleId);
        Assert.False(roleId.IsNullable);

        var code = entityType.FindProperty(nameof(WorkContext.Code));
        Assert.NotNull(code);
        Assert.False(code.IsNullable);
        Assert.Equal(80, code.GetMaxLength());

        var name = entityType.FindProperty(nameof(WorkContext.Name));
        Assert.NotNull(name);
        Assert.False(name.IsNullable);
        Assert.Equal(200, name.GetMaxLength());

        var isActive = entityType.FindProperty(nameof(WorkContext.IsActive));
        Assert.NotNull(isActive);
        Assert.False(isActive.IsNullable);

        var createdAt = entityType.FindProperty(nameof(WorkContext.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var roleFk = foreignKeys.First(fk =>
            fk.PrincipalEntityType.ClrType == typeof(Role));
        Assert.Equal(DeleteBehavior.Cascade, roleFk.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique
            && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(WorkContext.RoleId))
            && i.Properties.Any(p => p.Name == nameof(WorkContext.Code)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(WorkContext.RoleId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(WorkContext.Name)));
    }

    [Fact]
    public void WorkContexts_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.WorkContexts));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<WorkContext>), dbSetProperty.PropertyType);
    }

    // IW8-048: Skill, CredentialType, AssetType configuration tests

    [Fact]
    public void Skill_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(Skill));
        Assert.NotNull(entityType);

        Assert.Equal("Skills", entityType.GetTableName());

        var id = entityType.FindProperty(nameof(Skill.Id));
        Assert.NotNull(id);
        Assert.False(id.IsNullable);

        var code = entityType.FindProperty(nameof(Skill.Code));
        Assert.NotNull(code);
        Assert.False(code.IsNullable);
        Assert.Equal(50, code.GetMaxLength());

        var name = entityType.FindProperty(nameof(Skill.Name));
        Assert.NotNull(name);
        Assert.False(name.IsNullable);
        Assert.Equal(200, name.GetMaxLength());

        var description = entityType.FindProperty(nameof(Skill.Description));
        Assert.NotNull(description);
        Assert.True(description.IsNullable);
        Assert.Equal(1000, description.GetMaxLength());

        var parentId = entityType.FindProperty(nameof(Skill.ParentId));
        Assert.NotNull(parentId);
        Assert.True(parentId.IsNullable);

        var taxonomyVersionId = entityType.FindProperty(nameof(Skill.TaxonomyVersionId));
        Assert.NotNull(taxonomyVersionId);
        Assert.False(taxonomyVersionId.IsNullable);

        var isActive = entityType.FindProperty(nameof(Skill.IsActive));
        Assert.NotNull(isActive);
        Assert.False(isActive.IsNullable);

        var createdAt = entityType.FindProperty(nameof(Skill.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var parentFk = foreignKeys.FirstOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(Skill));
        Assert.NotNull(parentFk);
        Assert.Equal(DeleteBehavior.Restrict, parentFk!.DeleteBehavior);

        var taxVersionFk = foreignKeys.FirstOrDefault(fk =>
            fk.PrincipalEntityType.ClrType == typeof(TaxonomyVersion));
        Assert.NotNull(taxVersionFk);
        Assert.Equal(DeleteBehavior.Cascade, taxVersionFk!.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique
            && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(Skill.TaxonomyVersionId))
            && i.Properties.Any(p => p.Name == nameof(Skill.Code)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Skill.ParentId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Skill.TaxonomyVersionId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(Skill.Name)));
    }

    [Fact]
    public void Skills_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.Skills));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<Skill>), dbSetProperty.PropertyType);
    }

    [Fact]
    public void CredentialType_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(CredentialType));
        Assert.NotNull(entityType);

        Assert.Equal("CredentialTypes", entityType.GetTableName());

        var id = entityType.FindProperty(nameof(CredentialType.Id));
        Assert.NotNull(id);
        Assert.False(id.IsNullable);

        var code = entityType.FindProperty(nameof(CredentialType.Code));
        Assert.NotNull(code);
        Assert.False(code.IsNullable);
        Assert.Equal(50, code.GetMaxLength());

        var name = entityType.FindProperty(nameof(CredentialType.Name));
        Assert.NotNull(name);
        Assert.False(name.IsNullable);
        Assert.Equal(200, name.GetMaxLength());

        var description = entityType.FindProperty(nameof(CredentialType.Description));
        Assert.NotNull(description);
        Assert.True(description.IsNullable);
        Assert.Equal(1000, description.GetMaxLength());

        var taxonomyVersionId = entityType.FindProperty(nameof(CredentialType.TaxonomyVersionId));
        Assert.NotNull(taxonomyVersionId);
        Assert.False(taxonomyVersionId.IsNullable);

        var isActive = entityType.FindProperty(nameof(CredentialType.IsActive));
        Assert.NotNull(isActive);
        Assert.False(isActive.IsNullable);

        var createdAt = entityType.FindProperty(nameof(CredentialType.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var taxVersionFk = foreignKeys.First(fk =>
            fk.PrincipalEntityType.ClrType == typeof(TaxonomyVersion));
        Assert.Equal(DeleteBehavior.Cascade, taxVersionFk.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique
            && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(CredentialType.TaxonomyVersionId))
            && i.Properties.Any(p => p.Name == nameof(CredentialType.Code)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(CredentialType.TaxonomyVersionId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(CredentialType.Name)));
    }

    [Fact]
    public void CredentialTypes_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.CredentialTypes));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<CredentialType>), dbSetProperty.PropertyType);
    }

    [Fact]
    public void AssetType_ModelConfiguration_MatchesRequirements()
    {
        var entityType = _context.Model.FindEntityType(typeof(AssetType));
        Assert.NotNull(entityType);

        Assert.Equal("AssetTypes", entityType.GetTableName());

        var id = entityType.FindProperty(nameof(AssetType.Id));
        Assert.NotNull(id);
        Assert.False(id.IsNullable);

        var code = entityType.FindProperty(nameof(AssetType.Code));
        Assert.NotNull(code);
        Assert.False(code.IsNullable);
        Assert.Equal(50, code.GetMaxLength());

        var name = entityType.FindProperty(nameof(AssetType.Name));
        Assert.NotNull(name);
        Assert.False(name.IsNullable);
        Assert.Equal(200, name.GetMaxLength());

        var description = entityType.FindProperty(nameof(AssetType.Description));
        Assert.NotNull(description);
        Assert.True(description.IsNullable);
        Assert.Equal(1000, description.GetMaxLength());

        var taxonomyVersionId = entityType.FindProperty(nameof(AssetType.TaxonomyVersionId));
        Assert.NotNull(taxonomyVersionId);
        Assert.False(taxonomyVersionId.IsNullable);

        var isActive = entityType.FindProperty(nameof(AssetType.IsActive));
        Assert.NotNull(isActive);
        Assert.False(isActive.IsNullable);

        var createdAt = entityType.FindProperty(nameof(AssetType.CreatedAt));
        Assert.NotNull(createdAt);
        Assert.False(createdAt.IsNullable);

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var taxVersionFk = foreignKeys.First(fk =>
            fk.PrincipalEntityType.ClrType == typeof(TaxonomyVersion));
        Assert.Equal(DeleteBehavior.Cascade, taxVersionFk.DeleteBehavior);

        var indexes = entityType.GetIndexes().ToList();
        Assert.Contains(indexes, i => i.IsUnique
            && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(AssetType.TaxonomyVersionId))
            && i.Properties.Any(p => p.Name == nameof(AssetType.Code)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(AssetType.TaxonomyVersionId)));
        Assert.Contains(indexes, i => i.Properties.Any(p => p.Name == nameof(AssetType.Name)));
    }

    [Fact]
    public void AssetTypes_DbSet_IsRegisteredInAppDbContext()
    {
        var dbSetProperty = typeof(AppDbContext).GetProperty(nameof(AppDbContext.AssetTypes));
        Assert.NotNull(dbSetProperty);
        Assert.Equal(typeof(DbSet<AssetType>), dbSetProperty.PropertyType);
    }
}
