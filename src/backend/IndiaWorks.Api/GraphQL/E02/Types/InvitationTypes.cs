using HotChocolate.Types;
using IndiaWorks.Application.Features.E02.Implement;

namespace IndiaWorks.Api.GraphQL.E02.Types;

// ── OrganisationInvitationSummary ─────────────────────────────────────────────

public sealed class OrganisationInvitationSummary
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid InvitedByUserAccountId { get; set; }
    public string InviteeIdentifierType { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class OrganisationInvitationSummaryType : ObjectType<OrganisationInvitationSummary>
{
    protected override void Configure(IObjectTypeDescriptor<OrganisationInvitationSummary> descriptor)
    {
        descriptor.Field(s => s.Id).Type<NonNullType<IdType>>();
        descriptor.Field(s => s.OrganisationId).Type<NonNullType<IdType>>();
        descriptor.Field(s => s.InvitedByUserAccountId).Type<NonNullType<IdType>>();
        descriptor.Field(s => s.InviteeIdentifierType).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.Role).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.Status).Type<NonNullType<StringType>>();
        descriptor.Field(s => s.ExpiresAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(s => s.CreatedAt).Type<NonNullType<DateTimeType>>();
    }
}

// ── InviteMemberPayloadType ───────────────────────────────────────────────────

public sealed class InviteMemberPayloadType : ObjectType<InviteMemberResult>
{
    protected override void Configure(IObjectTypeDescriptor<InviteMemberResult> descriptor)
    {
        descriptor.Field(r => r.IsSuccess).Name("isSuccess").Type<NonNullType<BooleanType>>();
        descriptor.Field(r => r.InvitationId).Type<IdType>()
            .Resolve(ctx => ctx.Parent<InviteMemberResult>().IsSuccess
                ? ctx.Parent<InviteMemberResult>().InvitationId
                : null);
        descriptor.Field(r => r.ErrorCode).Type<StringType>()
            .Resolve(ctx => !ctx.Parent<InviteMemberResult>().IsSuccess
                ? ctx.Parent<InviteMemberResult>().ErrorCode
                : null);
        descriptor.Field(r => r.ErrorMessage).Type<StringType>()
            .Resolve(ctx => !ctx.Parent<InviteMemberResult>().IsSuccess
                ? ctx.Parent<InviteMemberResult>().ErrorMessage
                : null);
    }
}

// ── AcceptInvitationPayloadType ───────────────────────────────────────────────

public sealed class AcceptInvitationPayloadType : ObjectType<AcceptInvitationResult>
{
    protected override void Configure(IObjectTypeDescriptor<AcceptInvitationResult> descriptor)
    {
        descriptor.Field(r => r.IsSuccess).Name("isSuccess").Type<NonNullType<BooleanType>>();
        descriptor.Field(r => r.OrganisationId).Type<IdType>()
            .Resolve(ctx => ctx.Parent<AcceptInvitationResult>().IsSuccess
                ? ctx.Parent<AcceptInvitationResult>().OrganisationId
                : null);
        descriptor.Field(r => r.Role).Type<StringType>()
            .Resolve(ctx => ctx.Parent<AcceptInvitationResult>().IsSuccess
                ? ctx.Parent<AcceptInvitationResult>().Role?.ToString()
                : null);
        descriptor.Field(r => r.ErrorCode).Type<StringType>()
            .Resolve(ctx => !ctx.Parent<AcceptInvitationResult>().IsSuccess
                ? ctx.Parent<AcceptInvitationResult>().ErrorCode
                : null);
        descriptor.Field(r => r.ErrorMessage).Type<StringType>()
            .Resolve(ctx => !ctx.Parent<AcceptInvitationResult>().IsSuccess
                ? ctx.Parent<AcceptInvitationResult>().ErrorMessage
                : null);
    }
}

// ── RevokeInvitationPayloadType ───────────────────────────────────────────────

public sealed class RevokeInvitationPayloadType : ObjectType<RevokeInvitationResult>
{
    protected override void Configure(IObjectTypeDescriptor<RevokeInvitationResult> descriptor)
    {
        descriptor.Field(r => r.IsSuccess).Name("isSuccess").Type<NonNullType<BooleanType>>();
        descriptor.Field(r => r.ErrorCode).Type<StringType>()
            .Resolve(ctx => !ctx.Parent<RevokeInvitationResult>().IsSuccess
                ? ctx.Parent<RevokeInvitationResult>().ErrorCode
                : null);
        descriptor.Field(r => r.ErrorMessage).Type<StringType>()
            .Resolve(ctx => !ctx.Parent<RevokeInvitationResult>().IsSuccess
                ? ctx.Parent<RevokeInvitationResult>().ErrorMessage
                : null);
    }
}
