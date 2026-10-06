using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Application.Features.E02.Implement;

namespace IndiaWorks.Api.GraphQL.E02.Types;

public sealed class CreateOrganisationPayloadType : ObjectType<CreateOrganisationResult>
{
    protected override void Configure(IObjectTypeDescriptor<CreateOrganisationResult> descriptor)
    {
        descriptor.Field(r => r.IsSuccess).Name("isActive").Type<NonNullType<BooleanType>>();
        descriptor.Field(r => r.OrganisationId).Type<NonNullType<IdType>>()
            .Resolve(ctx => ctx.Parent<CreateOrganisationResult>().IsSuccess
                ? ctx.Parent<CreateOrganisationResult>().OrganisationId
                : null);
        descriptor.Field(r => r.LegalName).Type<StringType>()
            .Resolve(ctx => ctx.Parent<CreateOrganisationResult>().IsSuccess
                ? ctx.Parent<CreateOrganisationResult>().LegalName
                : null);
        descriptor.Field(r => r.TradeName).Type<StringType>()
            .Resolve(ctx => ctx.Parent<CreateOrganisationResult>().IsSuccess
                ? ctx.Parent<CreateOrganisationResult>().TradeName
                : null);
        descriptor.Field(r => r.Sector).Type<StringType>()
            .Resolve(ctx => ctx.Parent<CreateOrganisationResult>().IsSuccess
                ? ctx.Parent<CreateOrganisationResult>().Sector?.ToString()
                : null);
        descriptor.Field(r => r.EmployeeBand).Type<StringType>()
            .Resolve(ctx => ctx.Parent<CreateOrganisationResult>().IsSuccess
                ? ctx.Parent<CreateOrganisationResult>().EmployeeBand
                : null);
        descriptor.Field(r => r.Role).Type<StringType>()
            .Resolve(ctx => ctx.Parent<CreateOrganisationResult>().IsSuccess
                ? ctx.Parent<CreateOrganisationResult>().Role?.ToString()
                : null);
        descriptor.Field(r => r.CreatedAt).Type<DateTimeType>()
            .Resolve(ctx => ctx.Parent<CreateOrganisationResult>().IsSuccess
                ? ctx.Parent<CreateOrganisationResult>().CreatedAt
                : null);
        descriptor.Field(r => r.ErrorCode).Type<StringType>()
            .Resolve(ctx => !ctx.Parent<CreateOrganisationResult>().IsSuccess
                && !string.IsNullOrWhiteSpace(ctx.Parent<CreateOrganisationResult>().ErrorCode)
                ? ctx.Parent<CreateOrganisationResult>().ErrorCode
                : null);
        descriptor.Field(r => r.ErrorMessage).Type<StringType>()
            .Resolve(ctx => !ctx.Parent<CreateOrganisationResult>().IsSuccess
                && !string.IsNullOrWhiteSpace(ctx.Parent<CreateOrganisationResult>().ErrorMessage)
                ? ctx.Parent<CreateOrganisationResult>().ErrorMessage
                : null);
    }
}
