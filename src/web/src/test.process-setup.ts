// Storage shim for jsdom/Node environments that do not expose a usable
// localStorage/sessionStorage. Runs once when the module is loaded by each
// Vitest worker so every test in that worker shares the same in-memory store.
// TestBed configuration is handled per-spec by each test module.
import { beforeEach } from 'vitest';

// Created once per worker; reset between test files by setupFiles semantics.
const workerStore = new Map<string, string>();
const workerStorage = {
  getItem(key: string): string | null {
    return workerStore.get(key) ?? null;
  },
  setItem(key: string, value: string): void {
    workerStore.set(key, String(value));
  },
  removeItem(key: string): void {
    workerStore.delete(key);
  },
  clear(): void {
    workerStore.clear();
  },
  key(index: number): string | null {
    return Array.from(workerStore.keys())[index] ?? null;
  },
  get length(): number {
    return workerStore.size;
  },
} as Storage;

beforeEach(() => {
  // Re-attach the shared in-memory store before every test so jsdom/Node
  // environments that clobber globalThis.localStorage still see our shim.
  if (typeof globalThis === 'object') {
    Object.defineProperty(globalThis, 'localStorage', {
      value: workerStorage,
      writable: true,
      configurable: true,
    });
    Object.defineProperty(globalThis, 'sessionStorage', {
      value: workerStorage,
      writable: true,
      configurable: true,
    });
  }
  if (typeof global === 'object') {
    Object.defineProperty(global, 'localStorage', {
      value: workerStorage,
      writable: true,
      configurable: true,
    });
    Object.defineProperty(global, 'sessionStorage', {
      value: workerStorage,
      writable: true,
      configurable: true,
    });
  }
});
