# GraphQL and REST boundary

## GraphQL

Use for authenticated product workflows and structured queries/mutations:

- taxonomy/geography
- organisation/team
- jobs, search, facets, feed
- candidate profile
- applications
- candidate database/search/unlock/reveal
- CRM
- subscriptions/entitlements read model and checkout creation
- alerts, promotions, admin

## REST

Use for transport/provider boundaries:

- `/api/auth/otp/request`
- `/api/auth/otp/verify`
- `/api/auth/refresh`
- `/api/auth/logout`
- `/api/webhooks/payments/{provider}`
- `/health`, `/health/ready`
- future file upload/download endpoints
- public redirects only when browser/platform integration requires HTTP behavior

Do not duplicate the same business mutation in REST and GraphQL.
