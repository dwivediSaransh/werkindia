using IndiaWorks.Domain.E03;
using Xunit;

namespace IndiaWorks.Domain.Tests.E03;

public sealed class TaxonomyCoreEntityTests
{
    [Fact]
    public void TaxonomyVersion_Create_SetsProperties()
    {
        var version = TaxonomyVersion.Create(
            "1.0.0",
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            TaxonomyVersionStatus.Active,
            "Initial release");

        Assert.Equal("1.0.0", version.SemanticVersion);
        Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), version.EffectiveFrom);
        Assert.Equal(TaxonomyVersionStatus.Active, version.Status);
        Assert.Equal("Initial release", version.ReleaseNotes);
        Assert.NotEqual(default, version.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TaxonomyVersion_Create_NullOrWhitespaceSemanticVersion_Throws(string? semanticVersion)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            TaxonomyVersion.Create(semanticVersion!, DateTime.UtcNow, TaxonomyVersionStatus.Draft));

        Assert.Equal("semanticVersion", exception.ParamName);
    }

    [Fact]
    public void TaxonomyVersion_Create_NullReleaseNotes_KeepsItNull()
    {
        var version = TaxonomyVersion.Create("1.0.0", DateTime.UtcNow, TaxonomyVersionStatus.Draft, null);

        Assert.Null(version.ReleaseNotes);
    }

    [Fact]
    public void TaxonomyVersionStatus_IsDefinedForLifecycle()
    {
        var statuses = Enum.GetValues<TaxonomyVersionStatus>();

        Assert.Contains(TaxonomyVersionStatus.Draft, statuses);
        Assert.Contains(TaxonomyVersionStatus.Active, statuses);
        Assert.Contains(TaxonomyVersionStatus.Retired, statuses);
    }

    [Fact]
    public void JobFamily_Create_SetsProperties()
    {
        var family = JobFamily.Create("ENG", "Engineering", 1, isActive: true);

        Assert.Equal("ENG", family.Code);
        Assert.Equal("Engineering", family.Name);
        Assert.Equal(1L, family.TaxonomyVersionId);
        Assert.True(family.IsActive);
        Assert.Null(family.ParentId);
        Assert.NotEqual(default, family.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void JobFamily_Create_NullOrWhitespaceCode_Throws(string? code)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            JobFamily.Create(code!, "Name", 1));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void JobFamily_Create_SameCodeDifferentCase_TrimsAndUppercases()
    {
        var family = JobFamily.Create("eng", "Engineering", 1);

        Assert.Equal("ENG", family.Code);
    }

    [Fact]
    public void JobFamily_Create_WithParent_SetsParentId()
    {
        var parent = JobFamily.Create("ENG", "Engineering", 1);
        var child = JobFamily.Create("BACKEND", "Backend", 1, parent.Id);

        Assert.Equal(parent.Id, child.ParentId);
        Assert.Null(child.Parent);
    }

    [Fact]
    public void Role_Create_SetsProperties()
    {
        var role = Role.Create(
            "Delivery Driver",
            "delivery-driver",
            1,
            1,
            isActive: true);

        Assert.Equal("Delivery Driver", role.CanonicalName);
        Assert.Equal("delivery-driver", role.Slug);
        Assert.Equal(1L, role.JobFamilyId);
        Assert.Equal(1L, role.TaxonomyVersionId);
        Assert.True(role.IsActive);
        Assert.NotEqual(default, role.ValidFrom);
        Assert.Null(role.ValidTo);
        Assert.NotEqual(default, role.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Role_Create_NullOrWhitespaceCanonicalName_Throws(string? canonicalName)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            Role.Create(canonicalName!, "slug", 1, 1));

        Assert.Equal("canonicalName", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Role_Create_NullOrWhitespaceSlug_Throws(string? slug)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            Role.Create("Name", slug!, 1, 1));

        Assert.Equal("slug", exception.ParamName);
    }

    [Fact]
    public void Role_Create_SlugIsLowercased()
    {
        var role = Role.Create("Delivery Driver", "Delivery-Driver", 1, 1);

        Assert.Equal("delivery-driver", role.Slug);
    }

    [Fact]
    public void Role_Create_DefaultsValidFromToNow()
    {
        var before = DateTime.UtcNow;
        var role = Role.Create("Name", "slug", 1, 1);
        var after = DateTime.UtcNow;

        Assert.InRange(role.ValidFrom, before, after);
    }

    [Fact]
    public void RoleAlias_Create_SetsProperties()
    {
        var alias = RoleAlias.Create(
            1,
            "driver",
            languageCode: "en",
            regionCode: "IN",
            aliasType: "aka",
            weight: 1.5m);

        Assert.Equal(1L, alias.RoleId);
        Assert.Equal("driver", alias.Alias);
        Assert.Equal("EN", alias.LanguageCode);
        Assert.Equal("IN", alias.RegionCode);
        Assert.Equal("aka", alias.AliasType);
        Assert.Equal(1.5m, alias.Weight);
        Assert.False(alias.IsDeprecated);
        Assert.NotEqual(default, alias.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RoleAlias_Create_NullOrWhitespaceAlias_Throws(string? alias)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            RoleAlias.Create(1, alias!));

        Assert.Equal("alias", exception.ParamName);
    }

    [Fact]
    public void RoleAlias_Create_NullableCodes_KeepThemNull()
    {
        var alias = RoleAlias.Create(1, "driver", languageCode: null, regionCode: null);

        Assert.Null(alias.LanguageCode);
        Assert.Null(alias.RegionCode);
    }

    [Fact]
    public void Specialisation_Create_SetsProperties()
    {
        var spec = Specialisation.Create(1, "Frontend", "FE", isActive: true);

        Assert.Equal(1L, spec.RoleId);
        Assert.Equal("Frontend", spec.Name);
        Assert.Equal("FE", spec.Code);
        Assert.True(spec.IsActive);
        Assert.Null(spec.ParentId);
        Assert.NotEqual(default, spec.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Specialisation_Create_NullOrWhitespaceName_Throws(string? name)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            Specialisation.Create(1, name!, "CODE"));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Specialisation_Create_NullOrWhitespaceCode_Throws(string? code)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            Specialisation.Create(1, "Name", code!));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Specialisation_Create_CodeIsUppercased()
    {
        var spec = Specialisation.Create(1, "Frontend", "fe");

        Assert.Equal("FE", spec.Code);
    }

    [Fact]
    public void Specialisation_Create_WithParent_SetsParentId()
    {
        var parent = Specialisation.Create(1, "Parent", "PARENT");
        var child = Specialisation.Create(1, "Child", "CHILD", parent.Id);

        Assert.Equal(parent.Id, child.ParentId);
        Assert.Null(child.Parent);
    }

    [Fact]
    public void WorkContext_Create_SetsProperties()
    {
        var context = WorkContext.Create(1, "SHIFT_DAYTIME", "Daytime Shift", isActive: true);

        Assert.Equal(1L, context.RoleId);
        Assert.Equal("SHIFT_DAYTIME", context.Code);
        Assert.Equal("Daytime Shift", context.Name);
        Assert.True(context.IsActive);
        Assert.NotEqual(default, context.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WorkContext_Create_NullOrWhitespaceCode_Throws(string? code)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            WorkContext.Create(1, code!, "Name"));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WorkContext_Create_NullOrWhitespaceName_Throws(string? name)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            WorkContext.Create(1, "CODE", name!));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void WorkContext_Create_CodeIsUppercased()
    {
        var context = WorkContext.Create(1, "shift_daytime", "Daytime Shift");

        Assert.Equal("SHIFT_DAYTIME", context.Code);
    }

    [Fact]
    public void Role_RequiresBothForeignKeyIds()
    {
        var role = Role.Create("Name", "slug", 1, 2);

        Assert.Equal(1L, role.JobFamilyId);
        Assert.Equal(2L, role.TaxonomyVersionId);
    }

    // IW8-048: Skill, CredentialType, AssetType

    [Fact]
    public void Skill_Create_SetsProperties()
    {
        var skill = Skill.Create("PYTHON", "Python Programming", 1, isActive: true);

        Assert.Equal("PYTHON", skill.Code);
        Assert.Equal("Python Programming", skill.Name);
        Assert.True(skill.IsActive);
        Assert.Null(skill.ParentId);
        Assert.Null(skill.Description);
        Assert.NotEqual(default, skill.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Skill_Create_NullOrWhitespaceCode_Throws(string? code)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            Skill.Create(code!, "Name", 1));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Skill_Create_NullOrWhitespaceName_Throws(string? name)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            Skill.Create("CODE", name!, 1));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Skill_Create_SameCodeDifferentCase_TrimsAndUppercases()
    {
        var skill = Skill.Create("python", "Python", 1);

        Assert.Equal("PYTHON", skill.Code);
    }

    [Fact]
    public void Skill_Create_WithDescription_SetsDescription()
    {
        var skill = Skill.Create("PYTHON", "Python", 1, description: "A programming language");

        Assert.Equal("A programming language", skill.Description);
    }

    [Fact]
    public void Skill_Create_WithParent_SetsParentId()
    {
        var parent = Skill.Create("PROGRAMMING", "Programming", 1);
        var child = Skill.Create("PYTHON", "Python", 1, parent.Id);

        Assert.Equal(parent.Id, child.ParentId);
        Assert.Null(child.Parent);
    }

    [Fact]
    public void Skill_Create_WithTaxonomyVersion_SetsTaxonomyVersionId()
    {
        var skill = Skill.Create("PYTHON", "Python", 42);

        Assert.Equal(42L, skill.TaxonomyVersionId);
    }

    [Fact]
    public void CredentialType_Create_SetsProperties()
    {
        var credential = CredentialType.Create("BACHELORS", "Bachelors Degree", 1, isActive: true);

        Assert.Equal("BACHELORS", credential.Code);
        Assert.Equal("Bachelors Degree", credential.Name);
        Assert.True(credential.IsActive);
        Assert.Null(credential.Description);
        Assert.NotEqual(default, credential.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CredentialType_Create_NullOrWhitespaceCode_Throws(string? code)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            CredentialType.Create(code!, "Name", 1));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CredentialType_Create_NullOrWhitespaceName_Throws(string? name)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            CredentialType.Create("CODE", name!, 1));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void CredentialType_Create_SameCodeDifferentCase_TrimsAndUppercases()
    {
        var credential = CredentialType.Create("bachelors", "Bachelors", 1);

        Assert.Equal("BACHELORS", credential.Code);
    }

    [Fact]
    public void CredentialType_Create_WithDescription_SetsDescription()
    {
        var credential = CredentialType.Create("MD", "Medical Degree", 1, description: "Doctor of Medicine");

        Assert.Equal("Doctor of Medicine", credential.Description);
    }

    [Fact]
    public void CredentialType_Create_WithTaxonomyVersion_SetsTaxonomyVersionId()
    {
        var credential = CredentialType.Create("MD", "Medical Degree", 42);

        Assert.Equal(42L, credential.TaxonomyVersionId);
    }

    [Fact]
    public void AssetType_Create_SetsProperties()
    {
        var asset = AssetType.Create("DRIVERS_LICENSE", "Driver License", 1, isActive: true);

        Assert.Equal("DRIVERS_LICENSE", asset.Code);
        Assert.Equal("Driver License", asset.Name);
        Assert.True(asset.IsActive);
        Assert.Null(asset.Description);
        Assert.NotEqual(default, asset.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AssetType_Create_NullOrWhitespaceCode_Throws(string? code)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            AssetType.Create(code!, "Name", 1));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AssetType_Create_NullOrWhitespaceName_Throws(string? name)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            AssetType.Create("CODE", name!, 1));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void AssetType_Create_SameCodeDifferentCase_TrimsAndUppercases()
    {
        var asset = AssetType.Create("driver_license", "Driver License", 1);

        Assert.Equal("DRIVER_LICENSE", asset.Code);
    }

    [Fact]
    public void AssetType_Create_WithDescription_SetsDescription()
    {
        var asset = AssetType.Create("VEHICLE", "Personal Vehicle", 1, description: "Own car required");

        Assert.Equal("Own car required", asset.Description);
    }

    [Fact]
    public void AssetType_Create_WithTaxonomyVersion_SetsTaxonomyVersionId()
    {
        var asset = AssetType.Create("VEHICLE", "Vehicle", 42);

        Assert.Equal(42L, asset.TaxonomyVersionId);
    }
}
