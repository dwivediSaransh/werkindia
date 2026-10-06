import { test, expect } from "@playwright/test";

/**
 * IW8-035 E2E Tests:
 * Validates candidate OTP login, employer mobile/email OTP login,
 * and auth route guard redirects.
 */
test.describe("IndiaWorks Authentication Flows", () => {
  test("unauthenticated candidate route redirects to candidate otp-login with returnUrl", async ({
    page,
  }) => {
    await page.goto("/candidate/jobs");
    await expect(page).toHaveURL(/\/candidate\/otp-login/);
    await expect(page).toHaveURL(/returnUrl/);
    await expect(page.locator("h1")).toContainText(/Candidate Login/i);
  });

  test("unauthenticated employer route redirects to employer otp-login with returnUrl", async ({
    page,
  }) => {
    await page.goto("/employer");
    await expect(page).toHaveURL(/\/employer\/otp-login/);
    await expect(page).toHaveURL(/returnUrl/);
    await expect(page.locator("h1")).toContainText(/Employer Login/i);
  });

  test("candidate login screen validates phone input and sends OTP", async ({
    page,
  }) => {
    // Mock the backend OTP request
    await page.route("**/api/auth/otp/request", async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          challengeId: "11111111-1111-1111-1111-111111111111",
          purpose: "Login",
          expiresOn: new Date(Date.now() + 300_000).toISOString(),
          maxAttempts: 3,
          resendCooldownSeconds: 30,
        }),
      });
    });

    await page.goto("/candidate/otp-login");
    const phoneInput = page.locator("#phone");
    await expect(phoneInput).toBeVisible();

    // Type valid 10-digit number
    await phoneInput.fill("9876543210");
    const sendOtpButton = page.locator('button[type="submit"]');
    await expect(sendOtpButton).toBeEnabled();
    await sendOtpButton.click();

    // Assert OTP input step is shown
    const otpInput = page.locator("#otp");
    await expect(otpInput).toBeVisible();
    await expect(page.locator(".otp-message")).toContainText(/3210/);
  });

  test("employer login screen allows toggling between mobile and email", async ({
    page,
  }) => {
    await page.goto("/employer/otp-login");
    await expect(page.locator("h1")).toContainText(/Employer Login/i);

    // Initial is mobile
    await expect(page.locator("#mobile")).toBeVisible();

    // Toggle to email
    const emailToggle = page.locator('button:has-text("Email")');
    await emailToggle.click();
    await expect(page.locator("#email")).toBeVisible();
    await expect(page.locator("#mobile")).not.toBeVisible();

    // Fill valid email
    await page.locator("#email").fill("recruiter@example.com");
    await expect(page.locator('button[type="submit"]')).toBeEnabled();
  });
});
