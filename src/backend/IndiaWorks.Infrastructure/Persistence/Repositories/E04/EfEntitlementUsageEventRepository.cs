using IndiaWorks.Application.Features.E04.Implement;
using IndiaWorks.Domain.E04;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Repositories.E04;

public sealed class EfEntitlementUsageEventRepository(AppDbContext dbContext) : IEntitlementUsageEventRepository
{
    public Task AddAsync(EntitlementUsageEvent usageEvent, CancellationToken cancellationToken = default)
    {
        dbContext.EntitlementUsageEvents.Add(usageEvent);
        return Task.CompletedTask;
    }
}