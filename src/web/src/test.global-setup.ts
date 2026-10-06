// Vitest global setup: legacy entry kept for compat; the authoritative
// per-worker Storage shim and TestBed reset now live in src/test.process-setup.ts
// and are wired via vitest.config.ts setupFiles.
//
// NOTE(ponytail): test infra shim. If vitest/jsdom ships a real Storage and
// Angular TestBed reset is no longer needed here, remove this file and the
// globalSetup reference in vitest.config.ts.
