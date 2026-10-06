using IndiaWorks.Application.Features.E03.Implement;
using IndiaWorks.Domain.E03;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Repositories.E03;

public sealed class EfTaxonomyLookupRepository(AppDbContext dbContext) : ITaxonomyLookupRepository
{
    public async Task<IReadOnlyList<JobFamily>> GetJobFamiliesAsync(
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.JobFamilies
            .Include(f => f.Parent)
            .Include(f => f.TaxonomyVersion)
            .AsNoTracking();

        if (taxonomyVersionId.HasValue)
        {
            query = query.Where(f => f.TaxonomyVersionId == taxonomyVersionId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(f => f.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(f =>
                f.Name.Contains(term) ||
                f.Code.Contains(term));
        }

        query = query.OrderBy(f => f.Code);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(
        long? jobFamilyId = null,
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Roles
            .Include(r => r.JobFamily)
            .Include(r => r.TaxonomyVersion)
            .AsNoTracking();

        if (jobFamilyId.HasValue)
        {
            query = query.Where(r => r.JobFamilyId == jobFamilyId.Value);
        }

        if (taxonomyVersionId.HasValue)
        {
            query = query.Where(r => r.TaxonomyVersionId == taxonomyVersionId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.CanonicalName.Contains(term) ||
                r.Slug.Contains(term));
        }

        query = query.OrderBy(r => r.CanonicalName);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Specialisation>> GetSpecialisationsAsync(
        long? roleId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Specialisations
            .Include(s => s.Role)
            .AsNoTracking();

        if (roleId.HasValue)
        {
            query = query.Where(s => s.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Name.Contains(term) ||
                s.Code.Contains(term));
        }

        query = query.OrderBy(s => s.Name);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkContext>> GetWorkContextsAsync(
        long? roleId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.WorkContexts
            .Include(w => w.Role)
            .AsNoTracking();

        if (roleId.HasValue)
        {
            query = query.Where(w => w.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w =>
                w.Name.Contains(term) ||
                w.Code.Contains(term));
        }

        query = query.OrderBy(w => w.Name);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Skill>> GetSkillsAsync(
        long? taxonomyVersionId = null,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Skills
            .Include(s => s.Parent)
            .Include(s => s.TaxonomyVersion)
            .AsNoTracking();

        if (taxonomyVersionId.HasValue)
        {
            query = query.Where(s => s.TaxonomyVersionId == taxonomyVersionId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Name.Contains(term) ||
                s.Code.Contains(term));
        }

        query = query.OrderBy(s => s.Name);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxonomyVersion>> GetTaxonomyVersionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TaxonomyVersions
            .AsNoTracking()
            .OrderBy(v => v.SemanticVersion)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Geography>> GetGeographiesAsync(
        GeographyType? type = null,
        string? search = null,
        bool? hasCoordinates = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Geographies
            .Include(g => g.Parent)
            .AsNoTracking();

        if (type.HasValue)
        {
            query = query.Where(g => g.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(g =>
                g.Name.Contains(term) ||
                (g.Code != null && g.Code.Contains(term)));
        }

        if (hasCoordinates == true)
        {
            query = query.Where(g => g.Latitude.HasValue && g.Longitude.HasValue);
        }
        else if (hasCoordinates == false)
        {
            query = query.Where(g => !g.Latitude.HasValue || !g.Longitude.HasValue);
        }

        return await query
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }
}
