using Microsoft.Extensions.Logging;

namespace IndiaWorks.Application.Features.E01.Create;

public sealed class DevelopmentOtpSender(ILogger<DevelopmentOtpSender> logger) : IOtpSender
{
    public Task<OtpSendResult> SendAsync(OtpSendRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NormalizedIdentifier);

        if (request.Channel is not (OtpChannel.Sms or OtpChannel.Email or OtpChannel.Voice))
        {
            throw new ArgumentException("Unsupported channel.", nameof(request.Channel));
        }

        // Masked diagnostics only — never log plaintext OTP, tokens, or full contact values.
        var maskedAccountId = MaskIdentifier(request.AccountId.ToString());
        var maskedIdentifier = MaskIdentifier(request.NormalizedIdentifier);
        var channelLabel = request.Channel.ToString();

        logger.LogInformation(
            "OTP send simulated for account {AccountId} to {Identifier} via {Channel}",
            maskedAccountId,
            maskedIdentifier,
            channelLabel);

        // In development, always succeed with a deterministic message id for traceability.
        var result = new OtpSendResult(
            Success: true,
            ProviderMessageId: $"dev-{MaskIdentifier(request.AccountId.ToString())}-{request.Purpose}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            SentAt: DateTime.UtcNow);

        return Task.FromResult(result);
    }

    private static string MaskIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Show first and last character only; mask the middle.
        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        var visiblePrefix = Math.Max(1, value.Length / 4);
        var visibleSuffix = Math.Max(1, value.Length / 4);
        var maskedLength = value.Length - visiblePrefix - visibleSuffix;

        return value[..visiblePrefix] + new string('*', maskedLength) + value[^visibleSuffix..];
    }
}
