using IndiaWorks.Domain.E01;
using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E01.Implement;

/// <summary>
/// Determines available personas for an authenticated user and persists the active
/// persona on the user's session. Candidate vs employer availability is resolved from
/// domain state; the active persona is stored on <see cref="UserSession"/>.
/// </summary>
public sealed class PersonaResolutionService(
    IUserSessionRepository sessionRepo,
    ILogger<PersonaResolutionService> logger)
{
    /// <summary>
    /// Returns the active, non-revoked, non-expired sessions for the specified user.
    /// </summary>
    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        logger.LogDebug(
            "Fetching active sessions for user {UserId}",
            MaskUserId(userId));

        var sessions = await sessionRepo.GetAllByUserIdAsync(userId, cancellationToken);

        return sessions
            .Where(s => !s.IsRevoked && !s.IsExpired)
            .OrderByDescending(s => s.LastActivityOn)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Returns the personas the user may act as. A user is always eligible for the
    /// persona stored on any of their non-revoked, non-expired sessions; additional
    /// availability rules (for example CandidateProfile or OrganisationMembership) are
    /// applied downstream and wired in through <see cref="PersonaAvailability"/> without
    /// changing this contract.
    /// </summary>
    public async Task<PersonaResolutionResult> GetAvailablePersonasAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        logger.LogDebug(
            "Resolving available personas for user {UserId}",
            MaskUserId(userId));

        var activePersonas = new List<UserSession.Persona>();

        var sessions = await sessionRepo.GetAllByUserIdAsync(userId, cancellationToken);

        foreach (var session in sessions)
        {
            if (session.IsRevoked || session.IsExpired)
            {
                continue;
            }

            if (!activePersonas.Contains(session.ActivePersona))
            {
                activePersonas.Add(session.ActivePersona);
            }
        }

        if (activePersonas.Count == 0)
        {
            // No session-based evidence yet. The account is allowed to act as either
            // persona until downstream domain rules (CandidateProfile /
            // OrganisationMembership) narrow this down.
            activePersonas.Add(UserSession.Persona.Candidate);
            activePersonas.Add(UserSession.Persona.Employer);
        }

        var ordered = activePersonas.OrderBy(p => p).ToList().AsReadOnly();

        logger.LogDebug(
            "Available personas for user {UserId}: {Personas}",
            MaskUserId(userId),
            string.Join(", ", ordered.Select(p => p.ToString())));

        return PersonaResolutionResult.Ok(ordered);
    }

    /// <summary>
    /// Persists the requested persona as the active persona on the user's active session
    /// for the given device. Returns the new active persona.
    /// </summary>
    public async Task<PersonaResolutionResult> SwitchActivePersonaAsync(
        SwitchPersonaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(request.UserId));
        }

        if (request.DeviceId == Guid.Empty)
        {
            throw new ArgumentException("DeviceId is required.", nameof(request.DeviceId));
        }

        logger.LogDebug(
            "Switch persona requested for user {UserId} device {DeviceId} to {NewPersona}",
            MaskUserId(request.UserId),
            MaskDeviceId(request.DeviceId),
            request.NewPersona.ToString());

        var session = await sessionRepo.GetActiveByUserAndDeviceAsync(
            request.UserId,
            request.DeviceId,
            cancellationToken);

        if (session == null)
        {
            logger.LogWarning(
                "Switch persona failed: no active session for user {UserId} device {DeviceId}",
                MaskUserId(request.UserId),
                MaskDeviceId(request.DeviceId));

            return PersonaResolutionResult.Fail(
                "SESSION_REVOKED",
                "No active session found. Please re-authenticate.");
        }

        session.SwitchPersona(request.NewPersona);

        await sessionRepo.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Active persona switched for user {UserId} device {DeviceId} to {NewPersona}",
            MaskUserId(request.UserId),
            MaskDeviceId(request.DeviceId),
            request.NewPersona.ToString());

        return PersonaResolutionResult.OkActive(request.NewPersona);
    }

    private static string MaskUserId(Guid userId) =>
        userId.ToString("N")[..8] + "…";

    private static string MaskDeviceId(Guid deviceId) =>
        deviceId.ToString("N")[..8] + "…";
}
