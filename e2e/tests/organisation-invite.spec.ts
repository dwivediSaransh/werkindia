import { test, expect } from "@playwright/test";

/**
 * IW8-059 E2E Tests:
 * Validates organisation invitation flow: invite, list, revoke, accept.
 * Tests RBAC enforcement in UI.
 */
test.describe("IndiaWorks Organisation Invitation Flow", () => {
  test.beforeEach(async ({ page }) => {
    // Mock authentication - simulate logged in employer
    await page.route("**/graphql", async (route) => {
      const request = route.request();
      const body = request.postDataJSON();
      
      // Mock createOrganisation mutation
      if (body?.query?.includes("createOrganisation")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              createOrganisation: {
                isActive: true,
                organisationId: "11111111-1111-1111-1111-111111111111",
                legalName: "Test Organisation Pvt Ltd",
                tradeName: "Test Org",
                sector: "Private",
                employeeBand: "51-200",
                organisationType: "Private Limited",
                nic2025Code: "6201",
                role: "Owner",
                createdAt: new Date().toISOString(),
                errorCode: null,
                errorMessage: null
              }
            }
          })
        });
        return;
      }

      // Mock myOrganisations query
      if (body?.query?.includes("myOrganisations")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              myOrganisations: [{
                id: "11111111-1111-1111-1111-111111111111",
                legalName: "Test Organisation Pvt Ltd",
                tradeName: "Test Org",
                sector: "Private",
                employeeBand: "51-200",
                organisationType: "Private Limited",
                nic2025Code: "6201",
                verificationStatus: "Unverified",
                createdAt: new Date().toISOString(),
                role: "Owner"
              }]
            }
          })
        });
        return;
      }

      // Mock inviteMember mutation
      if (body?.query?.includes("inviteMember")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              inviteMember: {
                isSuccess: true,
                invitationId: "22222222-2222-2222-2222-222222222222",
                errorCode: null,
                errorMessage: null
              }
            }
          })
        });
        return;
      }

      // Mock pendingInvitations query
      if (body?.query?.includes("pendingInvitations")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              pendingInvitations: [{
                id: "22222222-2222-2222-2222-222222222222",
                organisationId: "11111111-1111-1111-1111-111111111111",
                inviteeIdentifierType: "Email",
                role: "Viewer",
                status: "Pending",
                expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
                createdAt: new Date().toISOString()
              }]
            }
          })
        });
        return;
      }

      // Mock revokeInvitation mutation
      if (body?.query?.includes("revokeInvitation")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              revokeInvitation: {
                isSuccess: true,
                errorCode: null,
                errorMessage: null
              }
            }
          })
        });
        return;
      }

      // Default fallback
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ data: {} })
      });
    });

    // Navigate to employer dashboard (simulates logged in state)
    await page.goto("/employer");
  });

  test("employer owner can invite a team member", async ({ page }) => {
    // Navigate to team management page
    await page.goto("/employer/team");
    
    // Verify page loads
    await expect(page.locator("h1")).toContainText(/Team/i);
    
    // Click invite button
    await page.click('button:has-text("Invite Member")');
    
    // Fill invite form
    await page.fill('#invitee-email', 'newmember@example.com');
    await page.selectOption('#invite-role', 'Viewer');
    
    // Submit
    await page.click('button[type="submit"]:has-text("Send Invite")');
    
    // Verify success toast/notification
    await expect(page.locator('.toast, .notification, .alert:has-text("invite")')).toBeVisible({ timeout: 5000 });
  });

  test("employer owner can view pending invitations", async ({ page }) => {
    await page.goto("/employer/team");
    
    // Wait for pending invitations list to load
    await expect(page.locator('[data-testid="pending-invitations"], .pending-invitations, table:has-text("Pending")')).toBeVisible({ timeout: 5000 });
    
    // Verify invitation details are shown (but NOT sensitive fields)
    const invitationRow = page.locator('tr:has-text("newmember@example.com"), .invitation-card:has-text("newmember@example.com")').first();
    await expect(invitationRow).toBeVisible();
    await expect(invitationRow).toContainText("Viewer");
    await expect(invitationRow).toContainText("Pending");
    
    // Verify sensitive fields are NOT displayed
    await expect(page).not.toContainText(/token/i);
    await expect(page).not.toContainText(/hash/i);
  });

  test("employer owner can revoke a pending invitation", async ({ page }) => {
    await page.goto("/employer/team");
    
    // Find the pending invitation and click revoke
    const revokeButton = page.locator('button:has-text("Revoke"), button[aria-label*="Revoke"]').first();
    await expect(revokeButton).toBeVisible();
    await revokeButton.click();
    
    // Confirm revocation if modal appears
    const confirmButton = page.locator('button:has-text("Confirm"), button:has-text("Yes"), button[aria-label*="Confirm"]');
    if (await confirmButton.isVisible({ timeout: 1000 })) {
      await confirmButton.click();
    }
    
    // Verify invitation removed from pending list
    await expect(page.locator('text="newmember@example.com"')).not.toBeVisible({ timeout: 5000 });
    await expect(page.locator('.toast, .notification, .alert:has-text("revoke")')).toBeVisible({ timeout: 5000 });
  });

  test("recruiter cannot access invite functionality", async ({ page }) => {
    // Mock recruiter login (different role)
    await page.route("**/graphql", async (route) => {
      const request = route.request();
      const body = request.postDataJSON();
      
      if (body?.query?.includes("myOrganisations")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              myOrganisations: [{
                id: "11111111-1111-1111-1111-111111111111",
                legalName: "Test Organisation Pvt Ltd",
                role: "Recruiter" // Recruiter role, not Owner
              }]
            }
          })
        });
        return;
      }
      
      // For inviteMember, return ROLE_NOT_ALLOWED
      if (body?.query?.includes("inviteMember")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              inviteMember: {
                isSuccess: false,
                errorCode: "ROLE_NOT_ALLOWED",
                errorMessage: "Only Owner and Admin roles can invite members"
              }
            }
          })
        });
        return;
      }

      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ data: {} }) });
    });

    await page.goto("/employer/team");
    
    // Invite button should be hidden or disabled for recruiters
    const inviteButton = page.locator('button:has-text("Invite Member")');
    await expect(inviteButton).not.toBeVisible(); // Or expect to be disabled
  });

  test("accept invitation flow works for invitee", async ({ page }) => {
    // Mock the accept invitation flow
    await page.route("**/graphql", async (route) => {
      const request = route.request();
      const body = request.postDataJSON();
      
      if (body?.query?.includes("acceptInvitation")) {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            data: {
              acceptInvitation: {
                isSuccess: true,
                organisationId: "11111111-1111-1111-1111-111111111111",
                role: "Viewer",
                errorCode: null,
                errorMessage: null
              }
            }
          })
        });
        return;
      }

      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ data: {} }) });
    });

    // Simulate invitee clicking acceptance link
    await page.goto("/invite/accept/22222222-2222-2222-2222-222222222222");
    
    // Verify acceptance page loads
    await expect(page.locator("h1")).toContainText(/Accept Invitation|Join Organisation/i);
    
    // Click accept button
    await page.click('button:has-text("Accept"), button[type="submit"]');
    
    // Verify success
    await expect(page.locator('.toast, .notification, .alert:has-text("joined"), .toast:has-text("accept")')).toBeVisible({ timeout: 5000 });
    
    // Verify redirected to employer dashboard
    await expect(page).toHaveURL(/\/employer/);
  });

  test("invitation form validates email input", async ({ page }) => {
    await page.goto("/employer/team");
    
    await page.click('button:has-text("Invite Member")');
    
    // Try submitting without email
    await page.click('button[type="submit"]:has-text("Send Invite")');
    
    // Should show validation error
    await expect(page.locator('.error, .validation-error, [aria-invalid="true"]')).toBeVisible({ timeout: 3000 });
    
    // Fill invalid email
    await page.fill('#invitee-email', 'not-an-email');
    await page.click('button[type="submit"]:has-text("Send Invite")');
    
    // Should show validation error
    await expect(page.locator('.error, .validation-error')).toBeVisible({ timeout: 3000 });
  });

  test("role selector shows correct RBAC options", async ({ page }) => {
    await page.goto("/employer/team");
    
    await page.click('button:has-text("Invite Member")');
    
    // Open role dropdown
    await page.click('#invite-role');
    
    // Verify available roles (Owner should not be selectable)
    const options = page.locator('option, [role="option"]');
    const optionTexts = await options.allTextContents();
    
    // Should have Viewer, Recruiter, Admin, BillingAdmin but NOT Owner
    expect(optionTexts).toContain("Viewer");
    expect(optionTexts).toContain("Recruiter");
    expect(optionTexts).toContain("Admin");
    expect(optionTexts).toContain("Billing Admin");
    expect(optionTexts).not.toContain("Owner");
  });

  test("responsive design works on mobile viewport", async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 390, height: 844 });
    
    await page.goto("/employer/team");
    
    // Verify key elements are accessible
    await expect(page.locator("h1")).toBeVisible();
    await expect(page.locator('button:has-text("Invite Member")')).toBeVisible();
    
    // Open invite modal/form
    await page.click('button:has-text("Invite Member")');
    
    // Verify form fields are accessible
    await expect(page.locator('#invitee-email')).toBeVisible();
    await expect(page.locator('#invite-role')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });
});