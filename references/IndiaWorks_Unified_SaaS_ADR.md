# IndiaWorks Unified SaaS - Architecture Decision Records v7

**Date:** 25 September 2026  
**Baseline:** v5 unified SaaS + v6 product/technical review + v7 commercial access rule.

## ADR-001 - Organisation is the commercial boundary

**Status:** Accepted.  
UserAccount is identity; Organisation owns subscription, jobs, recruiter membership, usage and candidate-access entitlement. A user may be both Candidate and Employer and may belong to multiple organisations.

## ADR-002 - Five free distinct candidate unlocks, then paywall

**Status:** Accepted.  
Each verified organisation/legal entity receives **5 lifetime free distinct candidate unlocks**. Masked search/applicant lists do not consume the allowance. Reopening the same candidate, viewing the same candidate from another recruiter, or seeing the same candidate against another job does not consume again. The **6th distinct candidate** requires an active paid/add-on `CANDIDATE_UNLOCK` entitlement. The allowance does not reset on month boundaries, recruiter changes, job creation, cancellation or re-sign-up.

**Rationale:** maximises employer evaluation while placing the monetisation boundary on a clear value event instead of search-result scrolling.

## ADR-003 - Candidate unlock and contact reveal are separate concepts

**Status:** Accepted.  
`OrganisationCandidateUnlock` grants full professional profile access. `CandidateContactReveal` exposes permitted contact data and is separately consent-checked/audited. For the first five free candidates, contact may be allowed without consuming another candidate unlock; paid plans may separately meter contact reveals.

## ADR-004 - Candidate privacy precedes payment

**Status:** Accepted.  
Candidate consent/discoverability, application context and block state are evaluated before commercial entitlement. Payment never overrides candidate privacy or legal/safety requirements.

## ADR-005 - Search remains masked and index-backed

**Status:** Accepted.  
Employer candidate search and applicant lists return masked cards. Candidate and job search use derived projections/indexes; contact PII and identity-document data are excluded from those projections.

## ADR-006 - Entitlements are centrally resolved

**Status:** Accepted.  
All protected operations (candidate unlock, contact reveal, active jobs, search quota, seats, promotions, export/API) use one EntitlementService and immutable usage events with idempotency keys.

## ADR-007 - Free allowance uses distinct organisation+candidate key

**Status:** Accepted.  
The database enforces unique `(OrganisationId, CandidateProfileId)` in `OrganisationCandidateUnlock`, preventing retries, multiple recruiters or multiple jobs from double-consuming the allowance.

## ADR-008 - Free allowance is legal-entity scoped

**Status:** Accepted.  
OrganisationCommercialAccessState may use verified commercial identity fingerprint/risk signals to prevent repeated organisation creation solely to obtain another five free candidates. Overrides are administrative and audited.

## ADR-009 - Job filtering is canonical and faceted

**Status:** Accepted.  
Work mode, employment type, experience, education/course, pay, shift, freshness, urgency, skills, credentials, assets, benefits, employer trust/source, industry and geo are independent filter axes. Contextual facet counts come from the search projection.

## ADR-010 - Promotion never bypasses relevance/eligibility

**Status:** Accepted.  
Ranking order: hard eligibility -> structured relevance -> quality/freshness -> bounded commercial promotion boost.

## ADR-011 - Job moderation and job lifecycle are separate state machines

**Status:** Accepted.  
A job may be PUBLISHED but PENDING_REVIEW; discoverability requires valid lifecycle state + approved moderation state.

## ADR-012 - CandidateApplication and EmployerCandidateRelationship coexist

**Status:** Accepted.  
`CandidateApplication` models candidate-to-job; `EmployerCandidateRelationship` models durable organisation-to-candidate recruiting CRM state across jobs.

## ADR-013 - OTP sessions are persistent/revocable

**Status:** Accepted.  
OTP challenges are short-lived/single-use. Refresh sessions are server-tracked, rotated and revocable. A single UserAccount can switch candidate/employer persona.

## ADR-014 - Alerts are saved-search executions, not raw notification preferences

**Status:** Accepted.  
`SavedJobSearch` stores canonical filter; `JobAlertSubscription` stores schedule/channel; `JobAlertRun/Delivery` store watermark/deduplication and delivery state.

## ADR-015 - Stable HTTPS deep links

**Status:** Accepted.  
Jobs use stable public links with Android App Links/iOS Universal Links and web fallback; no dependency on deprecated Firebase Dynamic Links.
