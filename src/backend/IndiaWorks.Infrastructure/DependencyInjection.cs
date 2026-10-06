using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Repositories;
using IndiaWorks.Infrastructure.Persistence.Repositories.E02;
using IndiaWorks.Infrastructure.Persistence.Repositories.E03;
using IndiaWorks.Infrastructure.Persistence.Repositories.E04;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Application.Features.E02.Implement;
using IndiaWorks.Application.Features.E03.Implement;
using IndiaWorks.Application.Features.E04.Implement;
using IndiaWorks.Application.Features.E04.Provision;
using IndiaWorks.Application.Features.E04.Receive;
using IndiaWorks.Application.Features.E04.Create;
using IndiaWorks.Infrastructure.Payments.E04;

namespace IndiaWorks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIndiaWorksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IndiaWorksDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'IndiaWorksDb' is required for IndiaWorks infrastructure.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IOtpChallengeRepository, OtpChallengeRepository>();
        services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IOrganisationRepository, EfOrganisationRepository>();
        services.AddScoped<IOrganisationMembershipRepository, EfOrganisationMembershipRepository>();
        services.AddScoped<IOrganisationInvitationRepository, EfOrganisationInvitationRepository>();
        services.AddScoped<ITaxonomyLookupRepository, EfTaxonomyLookupRepository>();
        services.AddScoped<IEntitlementEvaluatorRepository, EfEntitlementEvaluatorRepository>();
        services.AddScoped<IOrganisationCandidateUnlockRepository, EfOrganisationCandidateUnlockRepository>();
        services.AddScoped<IOrganisationCommercialAccessStateRepository, EfOrganisationCommercialAccessStateRepository>();
        services.AddScoped<ICandidatePaywallEventRepository, EfCandidatePaywallEventRepository>();
        services.AddScoped<IEntitlementUsageEventRepository, EfEntitlementUsageEventRepository>();
        services.AddScoped<IBillingWebhookInboxRepository, EfBillingWebhookInboxRepository>();
        services.AddScoped<IPaymentGateway, ConfiguredPaymentGateway>();

        return services;
    }
}
