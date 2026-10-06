using IndiaWorks.Domain.E04;
using IndiaWorks.Application.Features.E04.Receive;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Repositories.E04;

public sealed class EfBillingWebhookInboxRepository(AppDbContext dbContext) : IBillingWebhookInboxRepository
{
    public async Task<BillingWebhookInbox?> GetByProviderAndEventIdAsync(
        string provider,
        string providerEventId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.BillingWebhookInbox
            .FirstOrDefaultAsync(i => i.Provider == provider && i.ProviderEventId == providerEventId, cancellationToken);
    }

    public async Task AddAsync(BillingWebhookInbox inbox, CancellationToken cancellationToken = default)
    {
        await dbContext.BillingWebhookInbox.AddAsync(inbox, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
