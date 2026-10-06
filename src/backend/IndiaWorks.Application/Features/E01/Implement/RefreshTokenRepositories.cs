using IndiaWorks.Domain.E01;

namespace IndiaWorks.Application.Features.E01.Implement;

public interface IUserDeviceRepository
{
    Task<UserDevice?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
    Task<UserDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(UserDevice device, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IUserSessionRepository
{
    Task<UserSession?> GetActiveByUserAndDeviceAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSession>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
