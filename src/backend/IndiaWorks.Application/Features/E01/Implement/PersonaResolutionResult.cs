using IndiaWorks.Domain.E01;

namespace IndiaWorks.Application.Features.E01.Implement;

public sealed record PersonaResolutionResult(
    bool IsSuccess,
    IReadOnlyList<UserSession.Persona>? AvailablePersonas = null,
    UserSession.Persona? ActivePersona = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static PersonaResolutionResult Ok(IReadOnlyList<UserSession.Persona> availablePersonas) =>
        new(IsSuccess: true, AvailablePersonas: availablePersonas ?? []);

    public static PersonaResolutionResult OkActive(UserSession.Persona activePersona) =>
        new(IsSuccess: true, ActivePersona: activePersona);

    public static PersonaResolutionResult Fail(string errorCode, string errorMessage) =>
        new(IsSuccess: false, ErrorCode: errorCode, ErrorMessage: errorMessage);
}

public sealed record SwitchPersonaRequest(
    Guid UserId,
    Guid DeviceId,
    UserSession.Persona NewPersona);
