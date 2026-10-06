using HotChocolate.Types;
using IndiaWorks.Domain.E01;

namespace IndiaWorks.Api.GraphQL.E01.Types;

public sealed class UserSessionInfoType : ObjectType<UserSession>
{
    protected override void Configure(IObjectTypeDescriptor<UserSession> descriptor)
    {
        descriptor.Field(s => s.Id).Type<NonNullType<IdType>>();
        descriptor.Field(s => s.DeviceId).Type<NonNullType<IdType>>();
        descriptor.Field(s => s.ActivePersona).Type<NonNullType<UserSessionPersonaEnumType>>();
        descriptor.Field(s => s.CreatedOn).Type<NonNullType<DateTimeType>>();
        descriptor.Field(s => s.LastActivityOn).Type<NonNullType<DateTimeType>>();
        descriptor.Field(s => s.ExpiresOn).Type<NonNullType<DateTimeType>>();
        descriptor.Field(s => s.IsRevoked).Ignore();
        descriptor.Field(s => s.IsExpired).Ignore();
    }
}

public sealed class UserSessionPersonaEnumType : EnumType<UserSession.Persona>
{
    protected override void Configure(IEnumTypeDescriptor<UserSession.Persona> descriptor)
    {
        descriptor.Name("UserSessionPersona");
    }
}
