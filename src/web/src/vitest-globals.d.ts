// Shim Vitest globals for the Angular builder. The Vitest runner itself supplies
// these when globals:true (configured in src/vitest.config.ts), but the ng build
// / ng test TypeScript pipeline still needs a declaration so files under src/ that
// are not .spec.ts can reference afterEach / beforeEach / describe / it / expect
// without importing from 'vitest'.
declare global {
  var afterEach: typeof afterEach;
  var beforeEach: typeof beforeEach;
  var localStorage: Storage | null;
  var sessionStorage: Storage | null;
}

export {};
