using IndiaWorks.Domain.E01;

namespace IndiaWorks.Application.Features.E01.Implement;

public sealed record VerifyOtpRequest(
    Guid ChallengeId,
    string Code,
    string NormalizedIdentifier);
