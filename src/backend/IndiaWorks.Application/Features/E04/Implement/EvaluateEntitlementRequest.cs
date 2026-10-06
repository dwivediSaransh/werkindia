namespace IndiaWorks.Application.Features.E04.Implement;

public sealed record EvaluateEntitlementRequest(
    Guid OrganisationId,
    string EntitlementDefinitionCode);