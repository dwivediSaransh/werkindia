using HotChocolate;
using HotChocolate.Types;

namespace IndiaWorks.Api.GraphQL.E02.Types;

public sealed class OrganisationSummary
{
    public Guid Id { get; set; }
    public string LegalName { get; set; } = default!;
    public string? TradeName { get; set; }
    public string? Sector { get; set; }
    public string? EmployeeBand { get; set; }
    public string? OrganisationType { get; set; }
    public string? Nic2025Code { get; set; }
    public string VerificationStatus { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = default!;
}

public sealed class OrganisationSummaryType : ObjectType<OrganisationSummary>
{
    protected override void Configure(IObjectTypeDescriptor<OrganisationSummary> descriptor)
    {
        descriptor.Field(s => s.Id).Type<NonNullType<IdType>>();
        descriptor.Field(s => s.LegalName).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.TradeName).Type<StringType>();
        descriptor.Field(s => s.Sector).Type<StringType>();
        descriptor.Field(s => s.EmployeeBand).Type<StringType>();
        descriptor.Field(s => s.OrganisationType).Type<StringType>();
        descriptor.Field(s => s.Nic2025Code).Type<StringType>();
        descriptor.Field(s => s.VerificationStatus).Type<StringType>();
        descriptor.Field(s => s.CreatedAt).Type<DateTimeType>();
        descriptor.Field(s => s.Role).Type<StringType>();
    }
}
