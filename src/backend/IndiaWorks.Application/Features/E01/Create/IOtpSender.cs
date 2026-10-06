namespace IndiaWorks.Application.Features.E01.Create;

public sealed record OtpSendRequest(
    Guid AccountId,
    Domain.E01.OtpPurpose Purpose,
    string NormalizedIdentifier,
    OtpChannel Channel);

public enum OtpChannel
{
    Sms = 1,
    Email = 2,
    Voice = 3
}

public sealed record OtpSendResult(
    bool Success,
    string? ProviderMessageId,
    DateTime SentAt);

public interface IOtpSender
{
    Task<OtpSendResult> SendAsync(OtpSendRequest request, CancellationToken cancellationToken = default);
}
