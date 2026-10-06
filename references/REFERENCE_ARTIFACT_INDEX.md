# IndiaWorks Unified SaaS v7 - Artifact Index

Architecture freeze candidate: 25 September 2026.

## Primary v7 commercial rule

A verified organisation/legal entity receives 5 lifetime free DISTINCT candidate unlocks. Masked applicant/search cards do not consume the allowance. Reopening the same candidate, finding the same candidate through another job, or using another recruiter does not consume again. The 6th distinct candidate requires active paid/add-on `CANDIDATE_UNLOCK` entitlement. Candidate privacy/block/consent is evaluated before commercial entitlement.

## Final artifacts

- IndiaWorks_Unified_SaaS_BRD.docx / .pdf
- IndiaWorks_Unified_SaaS_FRD.docx / .pdf
- IndiaWorks_Unified_SaaS_DFD.docx / .pdf
- IndiaWorks_Unified_SaaS_Technical_Architecture.docx / .pdf
- IndiaWorks_Unified_SaaS_SQLServer.sql
- IndiaWorks_Unified_SaaS_GraphQL.graphql
- IndiaWorks_Unified_SaaS_Data_Model.xlsx
- IndiaWorks_Unified_SaaS_ADR.md
- IndiaWorks_Unified_SaaS_Requirements_Traceability.csv
- IndiaWorks_Unified_SaaS_Implementation_Backlog.csv
- IndiaWorks_Unified_SaaS_Architecture.md
- IndiaWorks_Unified_SaaS_Backlog_Dependency_Graph.png
- DFD_Level0_Context.png
- DFD_Level1_Platform.png
- DFD_Level2_Candidate_Paywall.png
- DFD_Level2_Candidate_Flow.png
- DFD_Level2_Employer_Flow.png

## Backlog validation

The backlog contains 87 stories across S0-S9. It was machine-validated for:

- no dependency cycles;
- no dependency on a later sprint;
- no dependency on a later story in the same sprint;
- P0 stories before P1/P2 stories within every sprint;
- explicit critical-path flag and agent package per story.

## Delivery order

S0 Foundation -> S1 Identity/Tenancy/Free allowance -> S2 Billing/Entitlements/Paywall -> S3 Employer Job Authoring -> S4 Candidate Discovery/Application -> S5 Applicant CRM + Paywall -> S6 Paid Candidate Database -> S7 Alerts/Promotion -> S8 Trust/Admin/Analytics -> S9 Hardening/UAT/Launch.
