namespace IndiaWorks.Domain.E02;

/// <summary>
/// Represents an invitation sent to a user to join an Organisation with a specific role.
/// The invitation is identified by a secure token hash. Invitee contact is stored as a
/// privacy-preserving hash (not plaintext). The lifecycle progresses:
/// Pending → Accepted | Revoked | Expired.
/// </summary>
public sealed class OrganisationInvitation
{
    /// <summary>Surrogate primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Organisation the invitee is being invited to join.</summary>
    public Guid OrganisationId { get; private set; }

    /// <summary>UserAccountId of the organisation member who created this invitation.</summary>
    public Guid InvitedByUserAccountId { get; private set; }

    /// <summary>
    /// Describes the type of identifier used to identify the invitee
    /// (e.g. "Email", "Phone"). Kept as a string to allow future extension without migration.
    /// </summary>
    public string InviteeIdentifierType { get; private set; } = default!;

    /// <summary>
    /// SHA-256 hash of the normalized invitee contact identifier.
    /// Never store the raw contact value.
    /// </summary>
    public byte[] InviteeIdentifierHash { get; private set; } = default!;

    /// <summary>Organisational role the invitee will receive upon acceptance.</summary>
    public OrganisationRole Role { get; private set; }

    /// <summary>
    /// Secure hash of the one-time invitation token sent to the invitee.
    /// Never store the token plaintext; only the hash is persisted.
    /// </summary>
    public byte[] TokenHash { get; private set; } = default!;

    /// <summary>Current lifecycle state of the invitation.</summary>
    public InvitationStatus Status { get; private set; }

    /// <summary>UTC timestamp after which the invitation is considered expired.</summary>
    public DateTime ExpiresAt { get; private set; }
    public void Expire() => ExpiresAt = DateTime.UtcNow.AddSeconds(-1);

    /// <summary>UTC timestamp when the invitation record was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UserAccountId of the user who accepted the invitation; null if not yet accepted.</summary>
    public Guid? AcceptedByUserAccountId { get; private set; }

    /// <summary>UTC timestamp when the invitation was accepted; null if not yet accepted.</summary>
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>UTC timestamp when the invitation was revoked; null if not revoked.</summary>
    public DateTime? RevokedAt { get; private set; }

    // EF Core requires a parameterless constructor for materialization.
    private OrganisationInvitation() { }

    /// <summary>
    /// Factory: create a new pending invitation.
    /// </summary>
    /// <param name="organisationId">Target organisation.</param>
    /// <param name="invitedByUserAccountId">Inviting member's account id.</param>
    /// <param name="inviteeIdentifierType">Type label of the invitee contact field (e.g. "Email").</param>
    /// <param name="inviteeIdentifierHash">SHA-256 hash of normalized invitee contact value.</param>
    /// <param name="role">Role to grant on acceptance.</param>
    /// <param name="tokenHash">Secure hash of the one-time token to be sent to the invitee.</param>
    /// <param name="expiresAt">UTC expiry date-time of this invitation.</param>
    public static OrganisationInvitation Create(
        Guid organisationId,
        Guid invitedByUserAccountId,
        string inviteeIdentifierType,
        byte[] inviteeIdentifierHash,
        OrganisationRole role,
        byte[] tokenHash,
        DateTime expiresAt)
    {
        if (organisationId == Guid.Empty)
            throw new ArgumentException("OrganisationId must not be empty.", nameof(organisationId));
        if (invitedByUserAccountId == Guid.Empty)
            throw new ArgumentException("InvitedByUserAccountId must not be empty.", nameof(invitedByUserAccountId));
        if (string.IsNullOrWhiteSpace(inviteeIdentifierType))
            throw new ArgumentException("InviteeIdentifierType must not be empty.", nameof(inviteeIdentifierType));
        if (inviteeIdentifierHash is null || inviteeIdentifierHash.Length == 0)
            throw new ArgumentException("InviteeIdentifierHash must not be empty.", nameof(inviteeIdentifierHash));
        if (tokenHash is null || tokenHash.Length == 0)
            throw new ArgumentException("TokenHash must not be empty.", nameof(tokenHash));
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("ExpiresAt must be in the future.", nameof(expiresAt));

        return new OrganisationInvitation
        {
            Id = Guid.NewGuid(),
            OrganisationId = organisationId,
            InvitedByUserAccountId = invitedByUserAccountId,
            InviteeIdentifierType = inviteeIdentifierType.Trim(),
            InviteeIdentifierHash = inviteeIdentifierHash,
            Role = role,
            TokenHash = tokenHash,
            Status = InvitationStatus.Pending,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Accept the invitation. Transitions: Pending → Accepted.
    /// </summary>
    public void Accept(Guid acceptedByUserAccountId)
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot accept invitation in status {Status}.");
        if (DateTime.UtcNow > ExpiresAt)
            throw new InvalidOperationException("Invitation has expired.");
        if (acceptedByUserAccountId == Guid.Empty)
            throw new ArgumentException("AcceptedByUserAccountId must not be empty.", nameof(acceptedByUserAccountId));

        Status = InvitationStatus.Accepted;
        AcceptedByUserAccountId = acceptedByUserAccountId;
        AcceptedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revoke the invitation. Transitions: Pending → Revoked.
    /// </summary>
    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot revoke invitation in status {Status}.");

        Status = InvitationStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the invitation is currently pending and not yet expired.
    /// </summary>
    public bool IsActive => Status == InvitationStatus.Pending && DateTime.UtcNow <= ExpiresAt;
}
