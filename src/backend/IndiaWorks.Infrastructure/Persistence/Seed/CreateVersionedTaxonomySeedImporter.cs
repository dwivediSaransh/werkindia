using System.Text.Json;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed.Models;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Seed;

public static partial class CreateVersionedTaxonomySeedImporter
{
    public static async Task SeedAsync(
        AppDbContext db,
        string seedJson,
        CancellationToken ct = default)
    {
        var dataset = JsonSerializer.Deserialize<TaxonomySeedDataset>(seedJson, JsonOptions);

        if (dataset is null)
        {
            throw new SeedDataException("Taxonomy seed dataset could not be deserialized.");
        }

        var semanticVersion = dataset.SemanticVersion.Trim();
        var effectiveFrom = ParseIsoDate(dataset.EffectiveFrom);
        var status = ParseTaxonomyVersionStatus(dataset.Status);
        var releaseNotes = string.IsNullOrWhiteSpace(dataset.ReleaseNotes)
            ? null
            : dataset.ReleaseNotes.Trim();

        var taxonomyVersion = TaxonomyVersion.Create(
            semanticVersion,
            effectiveFrom,
            status,
            releaseNotes);

        db.TaxonomyVersions.Add(taxonomyVersion);
        await db.SaveChangesAsync(ct);

        var versionId = taxonomyVersion.Id;

        var codeToId = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var slugToId = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var nameToId = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in dataset.Rows)
        {
            switch (row.EntityType)
            {
                case "JobFamily":
                    await ImportJobFamilyAsync(db, versionId, row, codeToId, slugToId, ct);
                    break;
                case "Role":
                    await ImportRoleAsync(db, versionId, row, slugToId, codeToId, ct);
                    break;
                case "Skill":
                    await ImportSkillAsync(db, versionId, row, codeToId, ct);
                    break;
                case "CredentialType":
                    await ImportCredentialTypeAsync(db, versionId, row, codeToId, ct);
                    break;
                case "AssetType":
                    await ImportAssetTypeAsync(db, versionId, row, codeToId, ct);
                    break;
                case "Specialisation":
                    await ImportSpecialisationAsync(db, versionId, row, slugToId, ct);
                    break;
                case "WorkContext":
                    await ImportWorkContextAsync(db, versionId, row, slugToId, ct);
                    break;
                case "RoleAlias":
                    await ImportRoleAliasAsync(db, versionId, row, slugToId, ct);
                    break;
                default:
                    throw new SeedDataException($"Unknown entityType '{row.EntityType}'.");
            }
        }
    }

    private static async Task ImportJobFamilyAsync(
        AppDbContext db,
        long versionId,
        TaxonomySeedRow row,
        Dictionary<string, long> codeToId,
        Dictionary<string, long> slugToId,
        CancellationToken ct)
    {
        var existing = await db.JobFamilies.AsNoTracking()
            .FirstOrDefaultAsync(j => j.TaxonomyVersionId == versionId && j.Code == row.Code, ct);

        if (existing is not null)
        {
            codeToId[row.Code] = existing.Id;
            return;
        }

        var parentId = row.ParentCode is null ? null : (long?)codeToId.GetValueOrDefault(row.ParentCode);
        var jobFamily = JobFamily.Create(row.Code, row.Name, versionId, parentId, row.IsActive);
        db.JobFamilies.Add(jobFamily);
        await db.SaveChangesAsync(ct);
        codeToId[row.Code] = jobFamily.Id;
        slugToId[row.Code] = jobFamily.Id;
    }

    private static async Task ImportRoleAsync(
        AppDbContext db,
        long versionId,
        TaxonomySeedRow row,
        Dictionary<string, long> slugToId,
        Dictionary<string, long> codeToId,
        CancellationToken ct)
    {
        if (row.Slug is null)
        {
            throw new SeedDataException($"Role row missing slug for code '{row.Code}'.");
        }

        var existing = await db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.TaxonomyVersionId == versionId && r.Slug == row.Slug!.ToLowerInvariant(), ct);

        if (existing is not null)
        {
            slugToId[row.Slug] = existing.Id;
            codeToId[row.Code] = existing.Id;
            return;
        }

        var jobFamilySlug = row.JobFamilySlug;
        if (jobFamilySlug is null)
        {
            throw new SeedDataException($"Role '{row.Code}' missing jobFamilySlug.");
        }

        if (!slugToId.TryGetValue(jobFamilySlug, out var jobFamilyId))
        {
            throw new SeedDataException($"Role '{row.Code}' references unknown jobFamilySlug '{jobFamilySlug}'.");
        }

        var role = Role.Create(
            row.CanonicalName ?? row.Name,
            row.Slug,
            jobFamilyId,
            versionId,
            row.IsActive,
            validFrom: DateTime.UtcNow,
            validTo: null);
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        slugToId[row.Slug] = role.Id;
        codeToId[row.Code] = role.Id;
    }

    private static async Task ImportSkillAsync(
        AppDbContext db,
        long versionId,
        TaxonomySeedRow row,
        Dictionary<string, long> codeToId,
        CancellationToken ct)
    {
        var existing = await db.Skills.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TaxonomyVersionId == versionId && s.Code == row.Code, ct);

        if (existing is not null)
        {
            codeToId[row.Code] = existing.Id;
            return;
        }

        long? parentId = null;
        if (row.ParentCode is not null)
        {
            parentId = codeToId.GetValueOrDefault(row.ParentCode);
        }

        var skill = Skill.Create(row.Code, row.Name, versionId, parentId, row.Description, row.IsActive);
        db.Skills.Add(skill);
        await db.SaveChangesAsync(ct);
        codeToId[row.Code] = skill.Id;
    }

    private static async Task ImportCredentialTypeAsync(
        AppDbContext db,
        long versionId,
        TaxonomySeedRow row,
        Dictionary<string, long> codeToId,
        CancellationToken ct)
    {
        var existing = await db.CredentialTypes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TaxonomyVersionId == versionId && c.Code == row.Code, ct);

        if (existing is not null)
        {
            codeToId[row.Code] = existing.Id;
            return;
        }

        var credentialType = CredentialType.Create(row.Code, row.Name, versionId, row.Description, row.IsActive);
        db.CredentialTypes.Add(credentialType);
        await db.SaveChangesAsync(ct);
        codeToId[row.Code] = credentialType.Id;
    }

    private static async Task ImportAssetTypeAsync(
        AppDbContext db,
        long versionId,
        TaxonomySeedRow row,
        Dictionary<string, long> codeToId,
        CancellationToken ct)
    {
        var existing = await db.AssetTypes.AsNoTracking()
            .FirstOrDefaultAsync(a => a.TaxonomyVersionId == versionId && a.Code == row.Code, ct);

        if (existing is not null)
        {
            codeToId[row.Code] = existing.Id;
            return;
        }

        var assetType = AssetType.Create(row.Code, row.Name, versionId, row.Description, row.IsActive);
        db.AssetTypes.Add(assetType);
        await db.SaveChangesAsync(ct);
        codeToId[row.Code] = assetType.Id;
    }

    private static async Task ImportSpecialisationAsync(
        AppDbContext db,
        long versionId,
        TaxonomySeedRow row,
        Dictionary<string, long> slugToId,
        CancellationToken ct)
    {
        if (row.Code is null)
        {
            throw new SeedDataException($"Specialisation row missing code for name '{row.Name}'.");
        }

        var jobFamilySlug = row.JobFamilySlug;
        if (jobFamilySlug is null)
        {
            throw new SeedDataException($"Specialisation '{row.Name}' missing jobFamilySlug.");
        }

        if (!slugToId.TryGetValue(jobFamilySlug, out var roleId))
        {
            throw new SeedDataException($"Specialisation '{row.Name}' references unknown slug '{jobFamilySlug}'.");
        }

        var existing = await db.Specialisations.AsNoTracking()
            .FirstOrDefaultAsync(s => s.RoleId == roleId
                && s.Code == row.Code.ToUpperInvariant(), ct);

        if (existing is not null)
        {
            return;
        }

        var specialisation = Specialisation.Create(roleId, row.Name, row.Code, null, row.IsActive);
        db.Specialisations.Add(specialisation);
        await db.SaveChangesAsync(ct);
    }

    private static async Task ImportWorkContextAsync(
        AppDbContext db,
        long versionId,
        TaxonomySeedRow row,
        Dictionary<string, long> slugToId,
        CancellationToken ct)
    {
        if (row.Code is null)
        {
            throw new SeedDataException($"WorkContext row missing code for name '{row.Name}'.");
        }

        var jobFamilySlug = row.JobFamilySlug;
        if (jobFamilySlug is null)
        {
            throw new SeedDataException($"WorkContext '{row.Name}' missing jobFamilySlug.");
        }

        if (!slugToId.TryGetValue(jobFamilySlug, out var roleId))
        {
            throw new SeedDataException($"WorkContext '{row.Name}' references unknown slug '{jobFamilySlug}'.");
        }

        var existing = await db.WorkContexts.AsNoTracking()
            .FirstOrDefaultAsync(w => w.RoleId == roleId
                && w.Code == row.Code.ToUpperInvariant(), ct);

        if (existing is not null)
        {
            return;
        }

        var workContext = WorkContext.Create(roleId, row.Code, row.Name, row.IsActive);
        db.WorkContexts.Add(workContext);
        await db.SaveChangesAsync(ct);
    }

    private static async Task ImportRoleAliasAsync(
        AppDbContext db,
        long versionId,
        TaxonomySeedRow row,
        Dictionary<string, long> slugToId,
        CancellationToken ct)
    {
        if (row.Alias is null)
        {
            throw new SeedDataException($"RoleAlias row missing alias for name '{row.Name}'.");
        }

        var jobFamilySlug = row.JobFamilySlug;
        if (jobFamilySlug is null)
        {
            throw new SeedDataException($"RoleAlias '{row.Alias}' missing jobFamilySlug.");
        }

        if (!slugToId.TryGetValue(jobFamilySlug, out var roleId))
        {
            throw new SeedDataException($"RoleAlias '{row.Alias}' references unknown slug '{jobFamilySlug}'.");
        }

        var existing = await db.RoleAliases.AsNoTracking()
            .FirstOrDefaultAsync(ra => ra.RoleId == roleId
                && ra.Alias == row.Alias.Trim(), ct);

        if (existing is not null)
        {
            return;
        }

        var roleAlias = RoleAlias.Create(roleId, row.Alias, row.LanguageCode, row.RegionCode, row.AliasType);
        db.RoleAliases.Add(roleAlias);
        await db.SaveChangesAsync(ct);
    }

    private static DateTime ParseIsoDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.UtcNow;
        }

        return DateTime.TryParse(value, out var dt)
            ? dt.ToUniversalTime()
            : throw new SeedDataException($"Invalid date '{value}'.");
    }

    private static TaxonomyVersionStatus ParseTaxonomyVersionStatus(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "draft" => TaxonomyVersionStatus.Draft,
            "active" => TaxonomyVersionStatus.Active,
            "retired" => TaxonomyVersionStatus.Retired,
            _ => throw new SeedDataException($"Unknown TaxonomyVersionStatus '{status}'.")
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
