using IndiaWorks.Domain.E01;

namespace IndiaWorks.Application.Features.E01.Implement;

public interface IOtpChallengeRepository
{
    Task AddAsync(OtpChallenge challenge, CancellationToken cancellationToken = default);
    Task<OtpChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OtpChallenge?> GetActiveByAccountAndPurposeAsync(
        Guid accountId,
        Domain.E01.OtpPurpose purpose,
        CancellationToken cancellationToken = default);
    Task<OtpChallenge?> GetAnyByAccountAndPurposeAsync(
        Guid accountId,
        Domain.E01.OtpPurpose purpose,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveByAccountAndPurposeAsync(
        Guid accountId,
        Domain.E01.OtpPurpose purpose,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
