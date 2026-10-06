namespace IndiaWorks.Contracts.Health;

public sealed record ServiceReadinessResponse(
    string Service,
    string Status,
    string Architecture);
