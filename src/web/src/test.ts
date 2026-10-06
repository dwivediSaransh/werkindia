// This file loads zone.js for fakeAsync/tick support in Angular tests.
import 'zone.js';
import 'zone.js/testing';

// Polyfill Storage before Angular test modules are configured so jsdom/Node
// environments that do not expose localStorage still have a working in-memory
// backing store for employer component specs.
//
// NOTE(ponytail): test infra shim. The per-worker TestBed.resetTestingModule()
// hook and the duplicate Storage patch were moved into src/test.process-setup.ts
// (wired via vitest.config.ts setupFiles) so Vitest's forks pool runs exactly one
// reset/Storage shim per worker. If vitest/jsdom ever supplies a real Storage and
// Angular no longer needs a reset, this file can be trimmed to just the zone.js
// imports above.
(function patchStorageIfMissing() {
  if (typeof globalThis === 'object' && !globalThis.localStorage) {
    const store = new Map<string, string>();
    const storage = {
      getItem(key: string): string | null {
        return store.get(key) ?? null;
      },
      setItem(key: string, value: string): void {
        store.set(key, String(value));
      },
      removeItem(key: string): void {
        store.delete(key);
      },
      clear(): void {
        store.clear();
      },
      key(index: number): string | null {
        return Array.from(store.keys())[index] ?? null;
      },
      get length(): number {
        return store.size;
      },
    } as Storage;
    globalThis.localStorage = storage;
    globalThis.sessionStorage = storage;
  }
})();
