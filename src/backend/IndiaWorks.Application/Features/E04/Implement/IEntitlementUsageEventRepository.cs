using IndiaWorks.Domain.E04;

namespace IndiaWorks.Application.Features.E04.Implement;

/// <summary>
/// Repository for recording EntitlementUsageEvent entities.
/// </summary>
public interface IEntitlementUsageEventRepository
{
    Task AddAsync(EntitlementUsageEvent usageEvent, CancellationToken cancellationToken = default);
}