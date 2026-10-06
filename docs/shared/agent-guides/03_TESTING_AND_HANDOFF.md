# Testing and agent handoff

## Minimum backend verification

```bash
dotnet restore
dotnet build IndiaWorks.slnx -c Release
dotnet test IndiaWorks.slnx -c Release --no-build
dotnet format --verify-no-changes
```

For migration stories:

```bash
dotnet ef migrations list --project src/backend/IndiaWorks.Infrastructure --startup-project src/backend/IndiaWorks.Api
dotnet ef database update --project src/backend/IndiaWorks.Infrastructure --startup-project src/backend/IndiaWorks.Api
```

## Minimum frontend verification

```bash
cd src/web
npm ci
npm run lint
npm test -- --watch=false
npm run build
```

For E2E stories:

```bash
npx playwright test
```

## Handoff response required from each coding agent

1. Files created/modified.
2. Short design summary.
3. Commands run and result.
4. Tests added.
5. Migration name, if any.
6. Known limitations/TODOs.
7. Any file changed outside the package target list and why.
8. Confirmation that acceptance checklist is complete.
