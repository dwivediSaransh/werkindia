using IndiaWorks.Api.Routing;
using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Contracts.E01;
using IndiaWorks.Domain.E01;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace IndiaWorks.Api.Rest.E01;

public static class ExposeEndpoints
{
    public static IEndpointRouteBuilder MapAuthOtpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth/otp");

        group.MapPost("/request", RequestOtp)
            .WithName(ApiEndpointNames.RequestOtp)
            .Produces<RequestOtpResponse>(StatusCodes.Status200OK)
            .Produces<RequestOtpErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<RequestOtpErrorResponse>(StatusCodes.Status429TooManyRequests);

        group.MapPost("/verify", VerifyOtp)
            .WithName(ApiEndpointNames.VerifyOtp)
            .Produces<VerifyResponse>(StatusCodes.Status200OK)
            .Produces<VerifyErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<VerifyErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", Refresh)
            .WithName(ApiEndpointNames.RefreshToken)
            .Produces<RefreshResponse>(StatusCodes.Status200OK)
            .Produces<RefreshErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<RefreshErrorResponse>(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .WithName(ApiEndpointNames.Logout)
            .Produces<LogoutResponse>(StatusCodes.Status200OK)
            .Produces<LogoutErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<LogoutErrorResponse>(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> RequestOtp(
        [FromBody] RequestOtpRequest request,
        RequestOtpUseCase useCase,
        CancellationToken cancellationToken)
    {
        // Validate payload
        if (request.AccountId == Guid.Empty)
        {
            return Results.BadRequest(new RequestOtpErrorResponse(OtpErrorCodes.ValidationFailed, "AccountId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.NormalizedIdentifier))
        {
            return Results.BadRequest(new RequestOtpErrorResponse(OtpErrorCodes.ValidationFailed, "NormalizedIdentifier is required."));
        }

        if (!Enum.TryParse<OtpPurpose>(request.Purpose, ignoreCase: true, out var purpose))
        {
            return Results.BadRequest(new RequestOtpErrorResponse(OtpErrorCodes.ValidationFailed, "Invalid Purpose. Supported values: Login, Verification."));
        }

        if (!Enum.TryParse<OtpChannel>(request.Channel, ignoreCase: true, out var channel))
        {
            return Results.BadRequest(new RequestOtpErrorResponse(OtpErrorCodes.ValidationFailed, "Invalid Channel. Supported values: Sms, Email, Voice."));
        }

        // Map to command and invoke handler
        var command = new RequestOtpCommand(
            AccountId: request.AccountId,
            Purpose: purpose,
            NormalizedIdentifier: request.NormalizedIdentifier,
            Channel: channel);

        var result = await useCase.ExecuteAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(MapSuccess(result.Challenge!))
            : MapFailure(result.ErrorCode!, result.ErrorMessage!);
    }

    private static RequestOtpResponse MapSuccess(OtpChallengeMetadata metadata)
    {
        return new RequestOtpResponse(
            ChallengeId: metadata.ChallengeId,
            Purpose: metadata.Purpose.ToString(),
            ExpiresOn: metadata.ExpiresOn,
            MaxAttempts: metadata.MaxAttempts,
            ResendCooldownSeconds: metadata.ResendCooldownRemaining.HasValue
                ? (int)metadata.ResendCooldownRemaining.Value.TotalSeconds
                : 0);
    }

    private static IResult MapFailure(string errorCode, string errorMessage)
    {
        var errorResponse = new RequestOtpErrorResponse(errorCode, errorMessage);
        return errorCode switch
        {
            OtpErrorCodes.RateLimited => TypedResults.Json(errorResponse, statusCode: StatusCodes.Status429TooManyRequests),
            _ => TypedResults.BadRequest(errorResponse)
        };
    }

    // ── /verify ────────────────────────────────────────────────────────

    private static async Task<IResult> VerifyOtp(
        [FromBody] VerifyRequest request,
        VerifyOtpUseCase verifyOtpUseCase,
        CompleteAuthUseCase completeAuthUseCase,
        CancellationToken cancellationToken)
    {
        if (request.ChallengeId == Guid.Empty)
            return Results.BadRequest(new VerifyErrorResponse(OtpErrorCodes.ValidationFailed, "ChallengeId is required."));

        if (string.IsNullOrWhiteSpace(request.Code))
            return Results.BadRequest(new VerifyErrorResponse(OtpErrorCodes.ValidationFailed, "Code is required."));

        if (string.IsNullOrWhiteSpace(request.NormalizedIdentifier))
            return Results.BadRequest(new VerifyErrorResponse(OtpErrorCodes.ValidationFailed, "NormalizedIdentifier is required."));

        var verifyResult = await verifyOtpUseCase.ExecuteAsync(
            new VerifyOtpRequest(
                ChallengeId: request.ChallengeId,
                Code: request.Code,
                NormalizedIdentifier: request.NormalizedIdentifier),
            cancellationToken);

        if (!verifyResult.IsSuccess)
            return Results.BadRequest(new VerifyErrorResponse(
                verifyResult.ErrorCode!,
                verifyResult.ErrorMessage!));

        if (!verifyResult.IsSuccess || verifyResult.AccountId == null || verifyResult.DisplayName == null
            || verifyResult.Provider == null || verifyResult.IsNewAccount == null)
            return Results.BadRequest(new VerifyErrorResponse(
                verifyResult.ErrorCode ?? "UNKNOWN",
                verifyResult.ErrorMessage ?? "Verification failed."));

        var completeResult = await completeAuthUseCase.ExecuteAsync(
            accountId: verifyResult.AccountId.Value,
            displayName: verifyResult.DisplayName!,
            provider: verifyResult.Provider!,
            isNewAccount: verifyResult.IsNewAccount.Value,
            cancellationToken: cancellationToken);

        return completeResult.IsSuccess
            ? Results.Ok(new VerifyResponse(
                AccountId: completeResult.AccountId!.Value,
                DisplayName: completeResult.DisplayName!,
                Provider: completeResult.Provider!,
                IsNewAccount: completeResult.IsNewAccount!.Value,
                DeviceId: completeResult.DeviceId!.Value,
                AccessToken: completeResult.AccessToken!,
                ExpiresOn: completeResult.ExpiresOn!.Value))
            : Results.Json(new VerifyErrorResponse(
                completeResult.ErrorCode!,
                completeResult.ErrorMessage!),
                statusCode: StatusCodes.Status401Unauthorized);
    }

    // ── /refresh ───────────────────────────────────────────────────────

    private static async Task<IResult> Refresh(
        [FromBody] RefreshRequest request,
        RefreshTokenRotationService refreshTokenRotationService,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            return Results.BadRequest(new RefreshErrorResponse(OtpErrorCodes.ValidationFailed, "UserId is required."));

        if (request.DeviceId == Guid.Empty)
            return Results.BadRequest(new RefreshErrorResponse(OtpErrorCodes.ValidationFailed, "DeviceId is required."));

        if (string.IsNullOrWhiteSpace(request.RefreshTokenHash))
            return Results.BadRequest(new RefreshErrorResponse(OtpErrorCodes.ValidationFailed, "RefreshTokenHash is required."));

        var result = await refreshTokenRotationService.RotateAsync(
            new RefreshTokenRotationRequest(
                UserId: request.UserId,
                DeviceId: request.DeviceId,
                RefreshTokenHash: request.RefreshTokenHash,
                CancellationToken: cancellationToken),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = MapRefreshStatusCode(result.ErrorCode!);
            return Results.Json(new RefreshErrorResponse(result.ErrorCode!, result.ErrorMessage!), statusCode: statusCode);
        }

        return Results.Ok(new RefreshResponse(
            AccessToken: result.AccessToken!,
            NewRefreshTokenHash: result.NewRefreshTokenHash!,
            ExpiresOn: DateTime.UtcNow.Add(JwtSettings.AccessTokenLifetime)));
    }

    // ── /logout ────────────────────────────────────────────────────────

    private static async Task<IResult> Logout(
        [FromBody] LogoutRequest request,
        IUserDeviceRepository deviceRepo,
        IUserSessionRepository sessionRepo,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            return Results.BadRequest(new LogoutErrorResponse(OtpErrorCodes.ValidationFailed, "UserId is required."));

        if (request.DeviceId == Guid.Empty)
            return Results.BadRequest(new LogoutErrorResponse(OtpErrorCodes.ValidationFailed, "DeviceId is required."));

        if (string.IsNullOrWhiteSpace(request.RefreshTokenHash))
            return Results.BadRequest(new LogoutErrorResponse(OtpErrorCodes.ValidationFailed, "RefreshTokenHash is required."));

        var device = await deviceRepo.GetByRefreshTokenHashAsync(request.RefreshTokenHash, cancellationToken);

        if (device == null)
            return Results.Json(new LogoutErrorResponse("UNAUTHENTICATED", "The refresh token is invalid or has expired. Please re-authenticate."), statusCode: StatusCodes.Status401Unauthorized);

        if (device.UserId != request.UserId)
            return Results.Json(new LogoutErrorResponse("FORBIDDEN", "The refresh token does not belong to this user."), statusCode: StatusCodes.Status401Unauthorized);

        device.Revoke();
        await deviceRepo.SaveChangesAsync(cancellationToken);

        var session = await sessionRepo.GetActiveByUserAndDeviceAsync(request.UserId, request.DeviceId, cancellationToken);
        if (session != null)
        {
            session.Revoke();
            await sessionRepo.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new LogoutResponse(Success: true));
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static int MapRefreshStatusCode(string errorCode) => errorCode switch
    {
        OtpErrorCodes.ValidationFailed => StatusCodes.Status400BadRequest,
        OtpErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
        OtpErrorCodes.SessionRevoked => StatusCodes.Status401Unauthorized,
        OtpErrorCodes.TokenReuseDetected => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status400BadRequest
    };

    private static readonly JwtSettings JwtSettings = new(
        Issuer: "IndiaWorks",
        Audience: "IndiaWorks",
        AccessTokenSeconds: 900);
}
