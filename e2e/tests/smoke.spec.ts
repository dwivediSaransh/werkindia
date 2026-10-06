import { test, expect } from '@playwright/test';

/**
 * IndiaWorks E2E smoke foundation.
 *
 * This is the baseline E2E test that validates the test runner, baseURL wiring and
 * the application's public-rendering boundary. Feature-specific E2E tests are added by
 * their owning stories; this file only establishes conventions.
 */
test.describe('IndiaWorks E2E foundations', () => {
  test('application base page is reachable', async ({ page }) => {
    // Act
    await page.goto('/');

    // Assert - the app bootstrap renders something useful rather than a blank document
    await expect(page).toHaveTitle(/IndiaWorks/i);
  });

  test('unknown route renders the not-found boundary', async ({ page }) => {
    // Act
    await page.goto('/this-route-does-not-exist-in-foundation');

    // Assert - a dedicated not-found page is shown instead of a raw client error
    const body = page.locator('body');
    await expect(body).toContainText(/not found/i);
  });
});
