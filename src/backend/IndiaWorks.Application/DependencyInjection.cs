using Microsoft.Extensions.DependencyInjection;
using IndiaWorks.Application.Features.E01.Create;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Application.Features.E04.Implement;
using IndiaWorks.Application.Features.E04.Provision;
using IndiaWorks.Application.Features.E04.Receive;

namespace IndiaWorks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIndiaWorksApplication(this IServiceCollection services)
    {
        services.AddSingleton<IOtpSender, DevelopmentOtpSender>();
        services.AddSingleton<IOtpRateLimiter, InMemoryOtpRateLimiter>();
        services.AddSingleton<IOtpHasher, Sha256OtpHasher>();
        services.AddSingleton(ProviderServices.OtpSettings);
        services.AddScoped<IOtpPolicyService, OtpPolicyService>();
        services.AddScoped<RequestOtpUseCase>();
        services.AddScoped<VerifyOtpUseCase>();
        services.AddScoped<CompleteAuthUseCase>();

        services.AddSingleton(ProviderServices.JwtSettings);
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<RefreshTokenRotationService>();
        services.AddScoped<PersonaResolutionService>();
        services.AddScoped<OrganisationCreationService>();
        services.AddScoped<OrganisationMemberInviteService>();
        services.AddScoped<OrganisationCommercialIdentityService>();
        services.AddScoped<EntitlementEvaluator>();
        services.AddScoped<ProvisionCandidateUnlockHandler>();
        services.AddScoped<ReceivePaymentWebhookHandler>();

        return services;
    }

    private static class ProviderServices
    {
        public static readonly OtpSettings OtpSettings = new();
        public static readonly JwtSettings JwtSettings = new(
            Issuer: "IndiaWorks",
            Audience: "IndiaWorks",
            AccessTokenSeconds: 900);
    }
}
