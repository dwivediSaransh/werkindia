# IW8-075: Expose payment webhook REST endpoint with inbox idempotency

Sprint: Sprint 3
Outcome: DONE

Recovered and completed IW8-075 after the native cron left unfinalized source changes. Payment webhook REST endpoint now verifies signatures through the payment gateway adapter, stores inbox rows before processing, handles duplicates idempotently, returns stable error codes, and keeps raw payloads out of persistence by storing payload hashes only.

```json
{
  "story_id": "IW8-075",
  "outcome": "DONE",
  "summary": "Recovered and completed IW8-075 after the native cron left unfinalized source changes. Payment webhook REST endpoint now verifies signatures through the payment gateway adapter, stores inbox rows before processing, handles duplicates idempotently, returns stable error codes, and keeps raw payloads out of persistence by storing payload hashes only.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\Program.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\Rest\\E04\\ExposeEndpoints.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Api\\Routing\\ApiEndpointNames.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\DependencyInjection.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Application\\Features\\E04\\Receive\\ReceivePaymentWebhookHandler.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Contracts\\E04\\PaymentWebhookRestContracts.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\DependencyInjection.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Payments\\E04\\ConfiguredPaymentGateway.cs",
    "C:\\Projects\\IndiaWorks\\src\\backend\\IndiaWorks.Infrastructure\\Persistence\\Repositories\\E04\\EfBillingWebhookInboxRepository.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.Application.Tests\\E04\\ReceivePaymentWebhookHandlerTests.cs",
    "C:\\Projects\\IndiaWorks\\tests\\IndiaWorks.IntegrationTests\\Rest\\E04\\ExposeEndpointTests.cs"
  ],
  "verification": [
    "d777ce0e3ff3: dotnet build IndiaWorks.slnx -c Release; exit=0; verifies AC1,AC3.",
    "39c6da98c494: dotnet test IndiaWorks.slnx -c Release --no-build; exit=0; verifies AC2,AC4; Passed 513, skipped 62, failed 0.",
    "b8c47d1daada: sensitive-term scan over IW8-075 endpoint/handler/contracts/adapters/tests; exit=0; verifies AC5.",
    "AC1: REST endpoint /api/webhooks/payments/{provider} delegates to ReceivePaymentWebhookHandler and returns stable success/error contracts.",
    "AC2: Application and integration tests cover happy path, duplicate event idempotency, and invalid signature failure.",
    "AC3: Provider failures use stable machine-readable codes including PAYMENT_WEBHOOK_SIGNATURE_INVALID and missing-secret failures fail closed.",
    "AC4: No IW8-075 schema change; implementation reuses IW8-074 BillingWebhookInbox repository and payload hash persistence.",
    "AC5: No forbidden sensitive OTP/token/password/Aadhaar/PAN/contact terms found in selected IW8-075 files."
  ],
  "known_gaps": [],
  "blocker": null,
  "notes": [
    "Host-generated inventory, proof records and outbox handoff.",
    "Corrected mislabeled native cron output that had attributed this work to IW8-076."
  ],
  "obsidian_status": "OUTBOX",
  "graphify_status": "OUTBOX",
  "no_source_changes_required": false
}
```

No MCP sync claimed. Contract/rollout details, if applicable, must be recorded in the summary or AC explanations.
