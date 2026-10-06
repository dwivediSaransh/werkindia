using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Api.GraphQL.E01.Types;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Api.GraphQL.E01;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class SessionQueries
{
    public async Task<IReadOnlyList<UserSession>> MeSessions(
        Guid userId,
        [Service] PersonaResolutionService personaService,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "UserId is required.");
        }

        return await personaService.GetActiveSessionsAsync(userId, cancellationToken);
    }
}
