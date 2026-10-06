using HotChocolate;
using HotChocolate.Types;
using IndiaWorks.Application.Features.E01.Implement;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Api.GraphQL.E01.Types;

public sealed class AuthSessionPayloadType : ObjectType<PersonaResolutionResult>
{
    protected override void Configure(IObjectTypeDescriptor<PersonaResolutionResult> descriptor)
    {
        descriptor.Field(r => r.IsSuccess).Name("isActive").Type<NonNullType<BooleanType>>();
        descriptor.Field(r => r.AvailablePersonas).Type<ListType<NonNullType<UserSessionPersonaEnumType>>>();
        descriptor.Field(r => r.ActivePersona).Type<UserSessionPersonaEnumType>()
            .Resolve(ctx =>
            {
                var result = ctx.Parent<PersonaResolutionResult>();
                return result.IsSuccess && result.ActivePersona.HasValue
                    ? result.ActivePersona.Value
                    : null;
            });
        descriptor.Field(r => r.ErrorCode).Type<StringType>()
            .Resolve(ctx =>
            {
                var result = ctx.Parent<PersonaResolutionResult>();
                return !result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorCode)
                    ? result.ErrorCode
                    : null;
            });
        descriptor.Field(r => r.ErrorMessage).Type<StringType>()
            .Resolve(ctx =>
            {
                var result = ctx.Parent<PersonaResolutionResult>();
                return !result.IsSuccess && !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : null;
            });
    }
}
