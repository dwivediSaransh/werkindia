using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace IndiaWorks.Application.Features.E01.Create;

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(CreateAccessTokenRequest request);
}

public sealed class JwtTokenService(ILogger<JwtTokenService> logger, JwtSettings settings)
    : IJwtTokenService
{
    private static readonly byte[] EmptyBytes = Array.Empty<byte>();

    public AccessTokenResult CreateAccessToken(CreateAccessTokenRequest request)
    {
        if (request.Subject == Guid.Empty)
        {
            return AccessTokenResult.Fail(
                "VALIDATION_FAILED",
                "Subject is required.");
        }

        if (request.SessionId == Guid.Empty)
        {
            return AccessTokenResult.Fail(
                "VALIDATION_FAILED",
                "SessionId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Persona))
        {
            return AccessTokenResult.Fail(
                "VALIDATION_FAILED",
                "Persona is required.");
        }

        try
        {
            var key = GetSigningKey(settings.Issuer);

            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, request.Subject.ToString("N")),
                new(JwtRegisteredClaimNames.Sid, request.SessionId.ToString("N")),
                new("persona", request.Persona),
                new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new(JwtRegisteredClaimNames.Iss, settings.Issuer),
                new(JwtRegisteredClaimNames.Aud, settings.Audience),
                new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(now.Add(settings.AccessTokenLifetime)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = now.Add(settings.AccessTokenLifetime),
                Issuer = settings.Issuer,
                Audience = settings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);

            logger.LogDebug(
                "Access token created for subject {Subject} session {SessionId} persona {Persona}",
                MaskGuid(request.Subject),
                MaskGuid(request.SessionId),
                MaskPersona(request.Persona));

            return AccessTokenResult.Ok(handler.WriteToken(token));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create access token for subject {Subject}", MaskGuid(request.Subject));
            return AccessTokenResult.Fail("UNAUTHENTICATED", "Failed to issue access token.");
        }
    }

    private static byte[] GetSigningKey(string issuer)
    {
        // Deterministic per-issuer key for development/testing.
        // Production should use a stable shared secret from configuration/secrets, not derive from the issuer string.
        var source = System.Text.Encoding.UTF8.GetBytes($"IndiaWorksJwtSigningKey-{issuer}");
        using var sha = SHA256.Create();
        return sha.ComputeHash(source);
    }

    private static string MaskGuid(Guid guid) => guid.ToString("N")[..8] + "…";
    private static string MaskPersona(string persona) => string.IsNullOrWhiteSpace(persona) ? string.Empty : "****";
}
