import { defineConfig } from 'vitest/config';
// npm test currently targets Angular 21.2.x / vitest 4.1.x with an installed
// @angular/build that only exports a public "./" and an internal "./private".
// Until the project upgrades/build tooling aligns on a supported @angular/build
// vite entrypoint, the vitest run bypasses the Angular Vite plugin and stays
// focused on the TypeScript/component unit tests below.
export default defineConfig({
  plugins: [],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test.process-setup.ts'],
    pool: 'forks',
    poolOptions: {
      forks: {
        maxThreads: 1,
        minThreads: 1,
      },
    },
  },
});
