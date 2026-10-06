using IndiaWorks.Domain.E01;

namespace IndiaWorks.Application.Features.E01.Implement;

public static class PersonaAvailability
{
    /// <summary>
    /// Determines which personas the user may act as.
    /// Candidate availability is true when the user has a candidate profile or has
    /// previously acted as a candidate (existing session history). Employer availability
    /// is true when the user belongs to at least one organisation.
    ///
    /// This is intentionally decoupled from CandidateProfile/OrganisationMembership entity
    /// types that do not exist yet in this sprint, so the contract stays stable when those
    /// entities are added later.
    /// </summary>
    public static bool CandidateAvailable(IReadOnlyList<UserSession.Persona> availablePersonas) =>
        availablePersonas.Contains(UserSession.Persona.Candidate);

    public static bool EmployerAvailable(IReadOnlyList<UserSession.Persona> availablePersonas) =>
        availablePersonas.Contains(UserSession.Persona.Employer);
}
