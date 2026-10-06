using IndiaWorks.Domain.E01;
using Microsoft.EntityFrameworkCore;
using IndiaWorks.Infrastructure.Persistence;

namespace IndiaWorks.Infrastructure.Persistence.Repositories;

public sealed class OtpChallengeRepository(AppDbContext db) : IndiaWorks.Application.Features.E01.Implement.IOtpChallengeRepository
{
    public async Task AddAsync(OtpChallenge challenge, CancellationToken cancellationToken = default)
    {
        db.OtpChallenges.Add(challenge);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task<OtpChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.OtpChallenges.FindAsync([id], cancellationToken);
    }

    public async Task<OtpChallenge?> GetActiveByAccountAndPurposeAsync(
        Guid accountId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await db.OtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.AccountId == accountId &&
                c.Purpose == purpose &&
                !c.IsConsumed &&
                !c.IsExpired,
                cancellationToken);
    }

    public async Task<OtpChallenge?> GetAnyByAccountAndPurposeAsync(
        Guid accountId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await db.OtpChallenges
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.AccountId == accountId &&
                c.Purpose == purpose,
                cancellationToken);
    }

    public async Task<bool> ExistsActiveByAccountAndPurposeAsync(
        Guid accountId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        return await db.OtpChallenges
            .AnyAsync(c =>
                c.AccountId == accountId &&
                c.Purpose == purpose &&
                !c.IsConsumed &&
                !c.IsExpired,
                cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await db.SaveChangesAsync(cancellationToken);
    }
}
