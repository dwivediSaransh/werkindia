using System.Text.Json;
using HotChocolate.AspNetCore;
using IndiaWorks.Api.GraphQL.E00;
using IndiaWorks.Api.GraphQL.E00.Types;
using IndiaWorks.Api.GraphQL.E01;
using IndiaWorks.Api.GraphQL.E01.Types;
using IndiaWorks.Api.GraphQL.E02;
using IndiaWorks.Api.GraphQL.E02.Types;
using IndiaWorks.Api.GraphQL.E03;
using IndiaWorks.Api.GraphQL.E03.Types;
using IndiaWorks.Api.Options;
using IndiaWorks.Api.Routing;
using IndiaWorks.Api.Rest.E01;
using IndiaWorks.Api.Rest.E04;
using IndiaWorks.Application;
using IndiaWorks.Contracts.Health;
using IndiaWorks.Domain;
using IndiaWorks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.UseUtcTimestamp = true;
});

builder.Services
    .AddOptions<IndiaWorksApiOptions>()
    .Bind(builder.Configuration.GetSection("IndiaWorks"))
    .ValidateDataAnnotations()
    .Validate(options => !string.IsNullOrWhiteSpace(options.ServiceName), "ServiceName is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ArchitectureBaseline), "ArchitectureBaseline is required.")
    .ValidateOnStart();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddIndiaWorksApplication();
builder.Services.AddIndiaWorksInfrastructure(builder.Configuration);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddErrorFilter<GraphQLErrorFilter>()
    .AddType<UserSessionInfoType>()
    .AddType<UserSessionPersonaEnumType>()
    .AddType<AuthSessionPayloadType>()
    .AddType<CreateOrganisationPayloadType>()
    .AddType<OrganisationSummaryType>()
    .AddType<EmployerSectorInputType>()
    .AddType<OrganisationInvitationSummaryType>()
    .AddType<InviteMemberPayloadType>()
    .AddType<AcceptInvitationPayloadType>()
    .AddType<RevokeInvitationPayloadType>()
    .AddTypeExtension<SessionQueries>()
    .AddTypeExtension<SessionMutations>()
    .AddTypeExtension<OrganisationQueries>()
    .AddTypeExtension<OrganisationMutations>()
    .AddTypeExtension<InvitationQueries>()
    .AddTypeExtension<InvitationMutations>()
    .AddTypeExtension<TaxonomyAndGeographyQueries>()
    .AddType<JobFamilySummaryType>()
    .AddType<RoleSummaryType>()
    .AddType<SpecialisationSummaryType>()
    .AddType<WorkContextSummaryType>()
    .AddType<SkillSummaryType>()
    .AddType<TaxonomyVersionSummaryType>()
    .AddType<GeographySummaryType>()
    .ModifyRequestOptions(options =>
    {
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });

var app = builder.Build();

app.UseExceptionHandler();
app.MapGet("/", () => Results.Redirect("/health"))
    .WithName("IndiaWorks.Root");
app.MapGet("/ready", (Microsoft.Extensions.Options.IOptions<IndiaWorksApiOptions> options) =>
{
    var value = options.Value;
    return Results.Ok(new ServiceReadinessResponse(
        value.ServiceName,
        "ready",
        value.ArchitectureBaseline));
}).WithName(ApiEndpointNames.Readiness);
app.MapGet("/health/ready", (Microsoft.Extensions.Options.IOptions<IndiaWorksApiOptions> options) =>
{
    var value = options.Value;
    return Results.Ok(new ServiceReadinessResponse(
        value.ServiceName,
        "ready",
        value.ArchitectureBaseline));
});
app.MapHealthChecks("/health")
    .WithName(ApiEndpointNames.Health);

app.MapAuthOtpEndpoints();
app.MapPaymentWebhookEndpoints();

app.MapGraphQL("/graphql");

if (builder.Environment.IsDevelopment())
{
    app.MapNitroApp("/graphql/ui", "/graphql");
}

app.Run();
