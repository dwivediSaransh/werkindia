namespace IndiaWorks.Domain.E02;

/// <summary>
/// RBAC roles for employer organisation members.
/// OWNER: Full control over organisation, legal profile, members, and billing.
/// ADMIN: Can manage members, jobs, and settings, except transferring ownership.
/// RECRUITER: Can create/manage jobs, unlock candidates, and review applications.
/// VIEWER: Read-only access to organisation data and jobs.
/// BILLING_ADMIN: Dedicated access to invoices, plans, and payment methods.
/// </summary>
public enum OrganisationRole
{
    Owner,
    Admin,
    Recruiter,
    Viewer,
    BillingAdmin
}
