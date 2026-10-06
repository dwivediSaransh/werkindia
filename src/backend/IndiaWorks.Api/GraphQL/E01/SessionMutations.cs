using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Api.GraphQL.E01;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class SessionMutations
{
    public async Task<PersonaResolutionResult> SwitchPersona(
        SwitchPersonaInput input,
        [Service] PersonaResolutionService personaService,
        CancellationToken cancellationToken)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(input.UserId), "UserId is required.");
        }

        if (input.DeviceId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(input.DeviceId), "DeviceId is required.");
        }

        if (!Enum.TryParse<UserSession.Persona>(input.NewPersona, ignoreCase: true, out var persona))
        {
            throw new ArgumentOutOfRangeException(nameof(input.NewPersona), "Invalid persona value. Supported: Candidate, Employer.");
        }

        return await personaService.SwitchActivePersonaAsync(
            new SwitchPersonaRequest(
                UserId: input.UserId,
                DeviceId: input.DeviceId,
                NewPersona: persona),
            cancellationToken);
    }
}

public sealed record SwitchPersonaInput(
    Guid UserId,
    Guid DeviceId,
    string NewPersona);
