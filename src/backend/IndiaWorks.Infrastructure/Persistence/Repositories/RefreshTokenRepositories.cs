using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Repositories;

public sealed class UserDeviceRepository(AppDbContext db) : IUserDeviceRepository
{
    public async Task<UserDevice?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        return await db.UserDevices
            .FirstOrDefaultAsync(d => d.RefreshTokenHash == refreshTokenHash, cancellationToken);
    }

    public async Task<UserDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.UserDevices.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(UserDevice device, CancellationToken cancellationToken = default)
    {
        db.UserDevices.Add(device);
        await SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class UserSessionRepository(AppDbContext db) : IUserSessionRepository
{
    public async Task<UserSession?> GetActiveByUserAndDeviceAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        return await db.UserSessions
            .FirstOrDefaultAsync(s =>
                s.UserId == userId &&
                s.DeviceId == deviceId &&
                !s.IsRevoked &&
                !s.IsExpired,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserSession>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await db.UserSessions
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        db.UserSessions.Add(session);
        await SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
