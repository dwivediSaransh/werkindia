using IndiaWorks.Domain.E03;

namespace IndiaWorks.Application.Features.E03.Implement;

public interface ITaxonomyLookupRepository
{
    Task<IReadOnlyList<JobFamily>> GetJobFamiliesAsync(
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetRolesAsync(
        long? jobFamilyId = null,
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Specialisation>> GetSpecialisationsAsync(
        long? roleId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkContext>> GetWorkContextsAsync(
        long? roleId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Skill>> GetSkillsAsync(
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxonomyVersion>> GetTaxonomyVersionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Geography>> GetGeographiesAsync(
        GeographyType? type = null,
        string? search = null,
        bool? hasCoordinates = null,
        CancellationToken cancellationToken = default);
}
