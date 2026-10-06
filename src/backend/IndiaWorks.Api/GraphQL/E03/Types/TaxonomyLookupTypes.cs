using HotChocolate;
using HotChocolate.Types;

namespace IndiaWorks.Api.GraphQL.E03.Types;

public sealed class JobFamilySummary
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? ParentCode { get; set; }
    public string? ParentName { get; set; }
    public long TaxonomyVersionId { get; set; }
    public string TaxonomyVersion { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class JobFamilySummaryType : ObjectType<JobFamilySummary>
{
    protected override void Configure(IObjectTypeDescriptor<JobFamilySummary> descriptor)
    {
        descriptor.Field(f => f.Id).Type<NonNullType<LongType>>();
        descriptor.Field(f => f.Code).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Name).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.ParentCode).Type<StringType>();
        descriptor.Field(f => f.ParentName).Type<StringType>();
        descriptor.Field(f => f.TaxonomyVersionId).Type<NonNullType<LongType>>();
        descriptor.Field(f => f.TaxonomyVersion).Type<StringType>();
        descriptor.Field(f => f.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(f => f.CreatedAt).Type<DateTimeType>();
    }
}

public sealed class RoleSummary
{
    public long Id { get; set; }
    public string CanonicalName { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public long JobFamilyId { get; set; }
    public string JobFamilyCode { get; set; } = default!;
    public string JobFamilyName { get; set; } = default!;
    public long TaxonomyVersionId { get; set; }
    public string TaxonomyVersion { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class RoleSummaryType : ObjectType<RoleSummary>
{
    protected override void Configure(IObjectTypeDescriptor<RoleSummary> descriptor)
    {
        descriptor.Field(r => r.Id).Type<NonNullType<LongType>>();
        descriptor.Field(r => r.CanonicalName).Type<NonNullType<StringType>>();
        descriptor.Field(r => r.Slug).Type<NonNullType<StringType>>();
        descriptor.Field(r => r.JobFamilyId).Type<NonNullType<LongType>>();
        descriptor.Field(r => r.JobFamilyCode).Type<StringType>();
        descriptor.Field(r => r.JobFamilyName).Type<StringType>();
        descriptor.Field(r => r.TaxonomyVersionId).Type<NonNullType<LongType>>();
        descriptor.Field(r => r.TaxonomyVersion).Type<StringType>();
        descriptor.Field(r => r.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(r => r.ValidFrom).Type<DateTimeType>();
        descriptor.Field(r => r.ValidTo).Type<DateTimeType>();
        descriptor.Field(r => r.CreatedAt).Type<DateTimeType>();
    }
}

public sealed class SpecialisationSummary
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public string RoleCanonicalName { get; set; } = default!;
    public string RoleSlug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? ParentCode { get; set; }
    public string? ParentName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SpecialisationSummaryType : ObjectType<SpecialisationSummary>
{
    protected override void Configure(IObjectTypeDescriptor<SpecialisationSummary> descriptor)
    {
        descriptor.Field(s => s.Id).Type<NonNullType<LongType>>();
        descriptor.Field(s => s.RoleId).Type<NonNullType<LongType>>();
        descriptor.Field(s => s.RoleCanonicalName).Type<StringType>();
        descriptor.Field(s => s.RoleSlug).Type<StringType>();
        descriptor.Field(s => s.Name).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.Code).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.ParentCode).Type<StringType>();
        descriptor.Field(s => s.ParentName).Type<StringType>();
        descriptor.Field(s => s.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(s => s.CreatedAt).Type<DateTimeType>();
    }
}

public sealed class WorkContextSummary
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public string RoleCanonicalName { get; set; } = default!;
    public string RoleSlug { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class WorkContextSummaryType : ObjectType<WorkContextSummary>
{
    protected override void Configure(IObjectTypeDescriptor<WorkContextSummary> descriptor)
    {
        descriptor.Field(w => w.Id).Type<NonNullType<LongType>>();
        descriptor.Field(w => w.RoleId).Type<NonNullType<LongType>>();
        descriptor.Field(w => w.RoleCanonicalName).Type<StringType>();
        descriptor.Field(w => w.RoleSlug).Type<StringType>();
        descriptor.Field(w => w.Code).Type<NonNullType<StringType>>();
        descriptor.Field(w => w.Name).Type<NonNullType<StringType>>();
        descriptor.Field(w => w.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(w => w.CreatedAt).Type<DateTimeType>();
    }
}

public sealed class SkillSummary
{
    public long Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ParentCode { get; set; }
    public string? ParentName { get; set; }
    public long TaxonomyVersionId { get; set; }
    public string TaxonomyVersion { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SkillSummaryType : ObjectType<SkillSummary>
{
    protected override void Configure(IObjectTypeDescriptor<SkillSummary> descriptor)
    {
        descriptor.Field(s => s.Id).Type<NonNullType<LongType>>();
        descriptor.Field(s => s.Code).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.Name).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.Description).Type<StringType>();
        descriptor.Field(s => s.ParentCode).Type<StringType>();
        descriptor.Field(s => s.ParentName).Type<StringType>();
        descriptor.Field(s => s.TaxonomyVersionId).Type<NonNullType<LongType>>();
        descriptor.Field(s => s.TaxonomyVersion).Type<StringType>();
        descriptor.Field(s => s.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(s => s.CreatedAt).Type<DateTimeType>();
    }
}

public sealed class TaxonomyVersionSummary
{
    public long Id { get; set; }
    public string SemanticVersion { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? ReleaseNotes { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class TaxonomyVersionSummaryType : ObjectType<TaxonomyVersionSummary>
{
    protected override void Configure(IObjectTypeDescriptor<TaxonomyVersionSummary> descriptor)
    {
        descriptor.Field(v => v.Id).Type<NonNullType<LongType>>();
        descriptor.Field(v => v.SemanticVersion).Type<NonNullType<StringType>>();
        descriptor.Field(v => v.Status).Type<NonNullType<StringType>>();
        descriptor.Field(v => v.ReleaseNotes).Type<StringType>();
        descriptor.Field(v => v.EffectiveFrom).Type<DateTimeType>();
        descriptor.Field(v => v.CreatedAt).Type<DateTimeType>();
    }
}
