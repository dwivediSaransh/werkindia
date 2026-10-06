using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Api.GraphQL.E03.Types;
using IndiaWorks.Application.Features.E03.Implement;
using IndiaWorks.Domain.E03;

namespace IndiaWorks.Api.GraphQL.E03;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class TaxonomyAndGeographyQueries
{
    public async Task<IReadOnlyList<JobFamilySummary>> JobFamilies(
        [Service] ITaxonomyLookupRepository repository,
        CancellationToken cancellationToken,
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null)
    {
        var families = await repository.GetJobFamiliesAsync(taxonomyVersionId, search, isActive, cancellationToken);
        return families.Select(MapJobFamily).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<RoleSummary>> Roles(
        [Service] ITaxonomyLookupRepository repository,
        CancellationToken cancellationToken,
        long? jobFamilyId = null,
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null)
    {
        var roles = await repository.GetRolesAsync(jobFamilyId, taxonomyVersionId, search, isActive, cancellationToken);
        return roles.Select(MapRole).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<SpecialisationSummary>> Specialisations(
        [Service] ITaxonomyLookupRepository repository,
        CancellationToken cancellationToken,
        long? roleId = null,
        string? search = null,
        bool? isActive = null)
    {
        var specialisations = await repository.GetSpecialisationsAsync(roleId, search, isActive, cancellationToken);
        return specialisations.Select(MapSpecialisation).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<WorkContextSummary>> WorkContexts(
        [Service] ITaxonomyLookupRepository repository,
        CancellationToken cancellationToken,
        long? roleId = null,
        string? search = null,
        bool? isActive = null)
    {
        var workContexts = await repository.GetWorkContextsAsync(roleId, search, isActive, cancellationToken);
        return workContexts.Select(MapWorkContext).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<SkillSummary>> Skills(
        [Service] ITaxonomyLookupRepository repository,
        CancellationToken cancellationToken,
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null)
    {
        var skills = await repository.GetSkillsAsync(taxonomyVersionId, search, isActive, cancellationToken);
        return skills.Select(MapSkill).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<TaxonomyVersionSummary>> TaxonomyVersions(
        [Service] ITaxonomyLookupRepository repository,
        CancellationToken cancellationToken)
    {
        var versions = await repository.GetTaxonomyVersionsAsync(cancellationToken);
        return versions.Select(MapTaxonomyVersion).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<GeographySummary>> Geographies(
        [Service] ITaxonomyLookupRepository repository,
        CancellationToken cancellationToken,
        Domain.E03.GeographyType? type = null,
        string? search = null,
        bool? hasCoordinates = null)
    {
        var geographies = await repository.GetGeographiesAsync(type, search, hasCoordinates, cancellationToken);
        return geographies.Select(MapGeography).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<GeographySummary>> GeographyHierarchy(
        [Service] ITaxonomyLookupRepository repository,
        CancellationToken cancellationToken,
        Domain.E03.GeographyType? type = null,
        string? search = null)
    {
        var geographies = await repository.GetGeographiesAsync(type, search, null, cancellationToken);
        return geographies.Select(MapGeography).ToList().AsReadOnly();
    }

    private static JobFamilySummary MapJobFamily(JobFamily f) => new()
    {
        Id = f.Id,
        Code = f.Code,
        Name = f.Name,
        ParentCode = f.Parent?.Code,
        ParentName = f.Parent?.Name,
        TaxonomyVersionId = f.TaxonomyVersionId,
        TaxonomyVersion = f.TaxonomyVersion?.SemanticVersion ?? "",
        IsActive = f.IsActive,
        CreatedAt = f.CreatedAt
    };

    private static RoleSummary MapRole(Role r) => new()
    {
        Id = r.Id,
        CanonicalName = r.CanonicalName,
        Slug = r.Slug,
        JobFamilyId = r.JobFamilyId,
        JobFamilyCode = r.JobFamily?.Code ?? "",
        JobFamilyName = r.JobFamily?.Name ?? "",
        TaxonomyVersionId = r.TaxonomyVersionId,
        TaxonomyVersion = r.TaxonomyVersion?.SemanticVersion ?? "",
        IsActive = r.IsActive,
        ValidFrom = r.ValidFrom,
        ValidTo = r.ValidTo,
        CreatedAt = r.CreatedAt
    };

    private static SpecialisationSummary MapSpecialisation(Specialisation s) => new()
    {
        Id = s.Id,
        RoleId = s.RoleId,
        RoleCanonicalName = s.Role?.CanonicalName ?? "",
        RoleSlug = s.Role?.Slug ?? "",
        Name = s.Name,
        Code = s.Code,
        ParentCode = s.Parent?.Code,
        ParentName = s.Parent?.Name,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt
    };

    private static WorkContextSummary MapWorkContext(WorkContext w) => new()
    {
        Id = w.Id,
        RoleId = w.RoleId,
        RoleCanonicalName = w.Role?.CanonicalName ?? "",
        RoleSlug = w.Role?.Slug ?? "",
        Code = w.Code,
        Name = w.Name,
        IsActive = w.IsActive,
        CreatedAt = w.CreatedAt
    };

    private static SkillSummary MapSkill(Skill s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        Name = s.Name,
        Description = s.Description,
        ParentCode = s.Parent?.Code,
        ParentName = s.Parent?.Name,
        TaxonomyVersionId = s.TaxonomyVersionId,
        TaxonomyVersion = s.TaxonomyVersion?.SemanticVersion ?? "",
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt
    };

    private static TaxonomyVersionSummary MapTaxonomyVersion(TaxonomyVersion v) => new()
    {
        Id = v.Id,
        SemanticVersion = v.SemanticVersion,
        Status = v.Status.ToString(),
        ReleaseNotes = v.ReleaseNotes,
        EffectiveFrom = v.EffectiveFrom,
        CreatedAt = v.CreatedAt
    };

    private static GeographySummary MapGeography(Domain.E03.Geography g) => new()
    {
        Id = g.Id,
        Type = g.Type.ToString(),
        Code = g.Code,
        Name = g.Name,
        ParentId = g.ParentId,
        ParentCode = g.Parent?.Code,
        ParentName = g.Parent?.Name,
        Latitude = g.Latitude,
        Longitude = g.Longitude,
        CreatedAt = g.CreatedAt
    };
}

public sealed class GeographySummary
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string? Code { get; set; }
    public string Name { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public string? ParentCode { get; set; }
    public string? ParentName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class GeographySummaryType : ObjectType<GeographySummary>
{
    protected override void Configure(IObjectTypeDescriptor<GeographySummary> descriptor)
    {
        descriptor.Field(g => g.Id).Type<NonNullType<IdType>>();
        descriptor.Field(g => g.Type).Type<NonNullType<StringType>>();
        descriptor.Field(g => g.Code).Type<StringType>();
        descriptor.Field(g => g.Name).Type<NonNullType<StringType>>();
        descriptor.Field(g => g.ParentId).Type<IdType>();
        descriptor.Field(g => g.ParentCode).Type<StringType>();
        descriptor.Field(g => g.ParentName).Type<StringType>();
        descriptor.Field(g => g.Latitude).Type<FloatType>();
        descriptor.Field(g => g.Longitude).Type<FloatType>();
        descriptor.Field(g => g.CreatedAt).Type<DateTimeType>();
    }
}
