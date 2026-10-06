# IW8-009: Create Angular application shell and lazy route groups

Sprint: Sprint 0
Outcome: DONE

IW8-009: Angular application shell and lazy route groups delivered. Created candidate/employer/admin/public lazy route groups plus shared error/not-found page components. Reused existing job-portal component as candidate jobs index. All three validation commands pass: lint, test (3 files, 11 tests), and build. SSR parameterized routes configured via server routes file.

```json
{
  "story_id": "IW8-009",
  "outcome": "DONE",
  "summary": "IW8-009: Angular application shell and lazy route groups delivered. Created candidate/employer/admin/public lazy route groups plus shared error/not-found page components. Reused existing job-portal component as candidate jobs index. All three validation commands pass: lint, test (3 files, 11 tests), and build. SSR parameterized routes configured via server routes file.",
  "files_changed": [
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\app.routes.server.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\app.routes.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\app.spec.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\admin\\admin.routes.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\admin\\pages\\admin-dashboard\\admin-dashboard.component.html",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\admin\\pages\\admin-dashboard\\admin-dashboard.component.scss",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\admin\\pages\\admin-dashboard\\admin-dashboard.component.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\candidate\\candidate.routes.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\candidate\\pages\\job-detail\\job-detail.component.html",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\candidate\\pages\\job-detail\\job-detail.component.scss",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\candidate\\pages\\job-detail\\job-detail.component.spec.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\candidate\\pages\\job-detail\\job-detail.component.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\employer.routes.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\pages\\employer-dashboard\\employer-dashboard.component.html",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\pages\\employer-dashboard\\employer-dashboard.component.scss",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\employer\\pages\\employer-dashboard\\employer-dashboard.component.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\public\\pages\\public-job\\public-job.component.html",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\public\\pages\\public-job\\public-job.component.scss",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\public\\pages\\public-job\\public-job.component.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\features\\public\\public.routes.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\shared\\pages\\error\\error.component.html",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\shared\\pages\\error\\error.component.scss",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\shared\\pages\\error\\error.component.ts",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\shared\\pages\\not-found\\not-found.component.html",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\shared\\pages\\not-found\\not-found.component.scss",
    "C:\\Projects\\IndiaWorks\\src\\web\\src\\app\\shared\\pages\\not-found\\not-found.component.ts"
  ],
  "verification": [
    "af87db86ca89: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=1; ACs=['AC1', 'AC8']; output=verification/af87db86ca89.log",
    "4d16d2ce4a88: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC8']; output=verification/4d16d2ce4a88.log",
    "592b16a59dc3: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'test', '--', '--watch=false']; exit=0; ACs=['AC1', 'AC8']; output=verification/592b16a59dc3.log",
    "bee685082c29: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'build']; exit=1; ACs=['AC1', 'AC8']; output=verification/bee685082c29.log",
    "46b81710e6d4: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'build']; exit=1; ACs=['AC1', 'AC8']; output=verification/46b81710e6d4.log",
    "6041c983c9d7: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'build']; exit=0; ACs=['AC1', 'AC8']; output=verification/6041c983c9d7.log",
    "ee53c8148146: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/ee53c8148146.log",
    "d4603c84d4ae: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'test', '--', '--watch=false']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/d4603c84d4ae.log",
    "c44bacf93ca3: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'build']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/c44bacf93ca3.log",
    "c5dd393efdac: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=1; ACs=['AC1', 'AC8']; output=verification/c5dd393efdac.log",
    "5ed684275a58: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC8']; output=verification/5ed684275a58.log",
    "0c5209d4a939: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'test', '--', '--watch=false']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/0c5209d4a939.log",
    "ea88c00fb261: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=1; ACs=['AC1', 'AC8']; output=verification/ea88c00fb261.log",
    "d7087f4d8ce0: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=1; ACs=['AC1', 'AC8']; output=verification/d7087f4d8ce0.log",
    "5c75959c04b4: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC8']; output=verification/5c75959c04b4.log",
    "4da189268337: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC8']; output=verification/4da189268337.log",
    "365c2ecea371: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'test', '--', '--watch=false']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/365c2ecea371.log",
    "eb2514de93d1: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC8']; output=verification/eb2514de93d1.log",
    "fa9ce0590df0: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'test', '--', '--watch=false']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/fa9ce0590df0.log",
    "d8cd4f9ee411: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC8']; output=verification/d8cd4f9ee411.log",
    "7c22d7fe322e: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'test', '--', '--watch=false']; exit=1; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/7c22d7fe322e.log",
    "5dc0408802c6: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'lint']; exit=0; ACs=['AC1', 'AC8']; output=verification/5dc0408802c6.log",
    "b297acd2ab26: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'test', '--', '--watch=false']; exit=0; ACs=['AC1', 'AC2', 'AC3', 'AC4', 'AC5', 'AC6', 'AC7', 'AC8', 'AC9']; output=verification/b297acd2ab26.log",
    "f105e0fed9f6: cwd=C:\\Projects\\IndiaWorks\\src\\web; argv=['C:\\\\nvm4w\\\\nodejs\\\\npm.CMD', 'run', 'build']; exit=0; ACs=['AC1', 'AC8']; output=verification/f105e0fed9f6.log",
    "AC1: route groups and error/not-found pages: candidate.routes.ts, employer.routes.ts, admin.routes.ts, public.routes.ts, NotFoundComponent, ErrorComponent all create/build without error; app.routes.ts registers 4 lazy groups + root not-found fixture. Proof: npm run build exit 0, verification/f105e0fed9f6.log.",
    "AC2: expected result/state: existing JobPortalComponent reused as candidate/jobs index with its own spec; new JobDetailComponent handles loading/error/success states; EmployerDashboardComponent and AdminDashboardComponent render with loading->success transitions. Test coverage: job-detail.component.spec.ts covers happy path + error + success + loading-prevention.",
    "AC3: soft mismatch / warning behavior: this story is frontend routing structure only; no backend application gating exists. Not applicable at this layer; AC3 from the candidate application domain story is not owned by this frontend shell story.",
    "AC4: hard requirement block: not owned by this story; frontend routing does not implement candidate application gating. Marked N/A for this layer.",
    "AC5: duplicate apply prevention: not owned by this story; frontend routing does not implement application submission. Marked N/A for this layer.",
    "AC6: job closes before submit: not owned by this story; frontend routing does not implement application submission. Marked N/A for this layer.",
    "AC7: no parallel domain concept: no new backend/domain/API operation introduced; only frontend route groups, pages, and shared error/not-found components added. Proof: no changes to backend, GraphQL, REST, or EF projects.",
    "AC8: tests pass and validation commands succeed: npm run lint exit 0 (verification/5dc0408802c6.log), npm test -- --watch=false exit 0 with 3 test files, 11 tests passed (verification/b297acd2ab26.log), npm run build exit 0 (verification/f105e0fed9f6.log).",
    "AC9: no sensitive data leakage: no logging statements added; no OTP/token/contact/identity values present in new components, routes, or styles; existing app logging unchanged. Proof: manual review of all created and modified files."
  ],
  "known_gaps": [],
  "blocker": null,
  "notes": [
    "Host-generated inventory, proof records and outbox handoff."
  ],
  "obsidian_status": "OUTBOX",
  "graphify_status": "OUTBOX"
}
```

No MCP sync claimed. Contract/rollout details, if applicable, must be recorded in the summary or AC explanations.
