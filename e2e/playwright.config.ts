import { defineConfig, devices } from '@playwright/test';

/**
 * IndiaWorks E2E test foundation.
 *
 * baseURL is read from INDIAWORKS_E2E_BASE_URL / INDIAWORKS_BASE_URL / localhost fallback.
 * Set INDIAWORKS_E2E_BASE_URL=https://indiaworks.example.com in CI or staging.
 */
const baseURL =
  process.env.INDIAWORKS_E2E_BASE_URL ??
  process.env.INDIAWORKS_BASE_URL ??
  'http://localhost:4200';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
  ],
  globalSetup: undefined,
  globalTeardown: undefined,
});
