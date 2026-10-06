namespace IndiaWorks.Domain.E02;

/// <summary>
/// Represents a user's membership and RBAC role within an employer organisation.
/// Composite primary key: (OrganisationId, UserAccountId).
/// </summary>
public sealed class OrganisationMembership
{
    public Guid OrganisationId { get; private set; }
    public Guid UserAccountId { get; private set; }
    public OrganisationRole Role { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private OrganisationMembership()
    {
    }

    public static OrganisationMembership Create(
        Guid organisationId,
        Guid userAccountId,
        OrganisationRole role = OrganisationRole.Recruiter)
    {
        if (organisationId == Guid.Empty)
        {
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));
        }

        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("UserAccountId must not be empty.", nameof(userAccountId));
        }

        return new OrganisationMembership
        {
            OrganisationId = organisationId,
            UserAccountId = userAccountId,
            Role = role,
            Status = MembershipStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ChangeRole(OrganisationRole newRole)
    {
        if (Status != MembershipStatus.Active)
        {
            throw new InvalidOperationException("Cannot change role for an inactive membership.");
        }

        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status == MembershipStatus.Terminated)
        {
            throw new InvalidOperationException("Cannot suspend a terminated membership.");
        }

        Status = MembershipStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (Status == MembershipStatus.Terminated)
        {
            throw new InvalidOperationException("Cannot reactivate a terminated membership.");
        }

        Status = MembershipStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Terminate()
    {
        Status = MembershipStatus.Terminated;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanManageTeam() =>
        Status == MembershipStatus.Active && (Role == OrganisationRole.Owner || Role == OrganisationRole.Admin);

    public bool CanManageBilling() =>
        Status == MembershipStatus.Active && (Role == OrganisationRole.Owner || Role == OrganisationRole.BillingAdmin);

    public bool CanManageJobs() =>
        Status == MembershipStatus.Active && (Role == OrganisationRole.Owner || Role == OrganisationRole.Admin || Role == OrganisationRole.Recruiter);
}
