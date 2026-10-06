using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Api.GraphQL.E02.Types;

namespace IndiaWorks.Api.GraphQL.E02;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class OrganisationQueries
{
    public async Task<IReadOnlyList<OrganisationSummary>> MyOrganisations(
        Guid userId,
        [Service] IOrganisationMembershipRepository membershipRepository,
        [Service] IOrganisationRepository organisationRepository,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("UserId is required.")
                .SetCode("VALIDATION_FAILED")
                .Build());
        }

        var memberships = await membershipRepository.GetByUserAccountIdAsync(userId, cancellationToken);
        if (memberships.Count == 0)
        {
            return [];
        }

        var organisationIds = memberships.Select(m => m.OrganisationId).Distinct().ToArray();
        var organisations = await organisationRepository.GetByIdsAsync(organisationIds, cancellationToken);
        var organisationsById = organisations.ToDictionary(o => o.Id);
        var result = new List<OrganisationSummary>(memberships.Count);

        foreach (var membership in memberships)
        {
            if (!organisationsById.TryGetValue(membership.OrganisationId, out var organisation))
            {
                continue;
            }

            result.Add(new OrganisationSummary
            {
                Id = organisation.Id,
                LegalName = organisation.LegalName,
                TradeName = organisation.TradeName,
                Sector = organisation.Sector?.ToString(),
                EmployeeBand = organisation.EmployeeBand,
                OrganisationType = organisation.OrganisationType,
                Nic2025Code = organisation.Nic2025Code,
                VerificationStatus = organisation.VerificationStatus.ToString(),
                CreatedAt = organisation.CreatedAt,
                Role = membership.Role.ToString()
            });
        }

        return result;
    }
}
