import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { TitleCasePipe } from '@angular/common';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ApolloTestingModule, ApolloTestingController } from 'apollo-angular/testing';
import { BuildEmployerTeamInvitationUiComponent } from './build-employer-team-invitation-ui.component';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

// Persistent in-memory storage shared across tests in this worker.
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

describe('BuildEmployerTeamInvitationUiComponent', () => {
  let fixture: ComponentFixture<BuildEmployerTeamInvitationUiComponent>;
  let component: BuildEmployerTeamInvitationUiComponent;
  let apolloTesting: ApolloTestingController;

  beforeEach(() => {
    if (typeof globalThis === 'object') {
      globalThis.localStorage = workerStorage;
      globalThis.sessionStorage = workerStorage;
    }
    if (typeof global === 'object') {
      (global as any).localStorage = workerStorage;
      (global as any).sessionStorage = workerStorage;
    }
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ApolloTestingModule,
        BuildEmployerTeamInvitationUiComponent,
        FormsModule,
        HttpClientTestingModule,
        TitleCasePipe,
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    fixture = TestBed.createComponent(BuildEmployerTeamInvitationUiComponent);
    component = fixture.componentInstance;
    apolloTesting = TestBed.inject(ApolloTestingController);
  });

  afterEach(() => {
    apolloTesting.verify();
    try {
      TestBed.resetTestingModule();
    } catch {
      /* ignore */
    }
  });

  // ── initial state ──────────────────────────────────────────────────────

  describe('initial state', () => {
    it('shows loading state initially', async () => {
      localStorage.setItem(
        'indiaworks_session',
        JSON.stringify({
          organisationId: 'org-1',
          userId: 'user-1',
        }),
      );
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('PendingInvitations');
      op.flush({ data: { pendingInvitations: [] } });
      await fixture.whenStable();
      fixture.detectChanges();

      expect(component.showLoading()).toBe(false);
      expect(component.showListing()).toBe(true);
      expect(component.vm().state).toBe('listing');
      expect(component.vm().invitations).toEqual([]);
    });
  });

  // ── listing after load ─────────────────────────────────────────────────

  describe('listing state', () => {
    beforeEach(() => {
      // Simulate session presence by setting localStorage before load.
      localStorage.setItem(
        'indiaworks_session',
        JSON.stringify({
          organisationId: 'org-1',
          userId: 'user-1',
        }),
      );
    });

    afterEach(() => {
      localStorage.removeItem('indiaworks_session');
    });

    it('loads pending invitations and renders listing', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('PendingInvitations');
      op.flush({
        data: {
          pendingInvitations: [
            {
              id: 'inv-1',
              organisationId: 'org-1',
              invitedByUserAccountId: 'user-1',
              inviteeIdentifierType: 'Email',
              role: 'Viewer',
              status: 'Pending',
              expiresAt: '2026-10-07T00:00:00Z',
              createdAt: '2026-09-30T00:00:00Z',
            },
          ],
        },
      });

      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 25));
      fixture.detectChanges();

      expect(component.showListing()).toBe(true);
      expect(component.showLoading()).toBe(false);
      expect(component.vm().invitations).toHaveLength(1);
      expect(component.pendingInvitations()).toHaveLength(1);
      expect(component.vm().invitations[0].role).toBe('Viewer');
    });

    it('separates pending and non-pending invitations', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('PendingInvitations');
      op.flush({
        data: {
          pendingInvitations: [
            {
              id: 'inv-1',
              organisationId: 'org-1',
              invitedByUserAccountId: 'user-1',
              inviteeIdentifierType: 'Email',
              role: 'Viewer',
              status: 'Pending',
              expiresAt: '2026-10-07T00:00:00Z',
              createdAt: '2026-09-30T00:00:00Z',
            },
            {
              id: 'inv-2',
              organisationId: 'org-1',
              invitedByUserAccountId: 'user-1',
              inviteeIdentifierType: 'Email',
              role: 'Admin',
              status: 'Acknowledged',
              expiresAt: '2026-10-07T00:00:00Z',
              createdAt: '2026-09-29T00:00:00Z',
            },
          ],
        },
      });

      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 25));
      fixture.detectChanges();

      expect(component.pendingInvitations()).toHaveLength(1);
      expect(component.invitedMembers()).toHaveLength(1);
      expect(component.pendingInvitations()[0].status).toBe('Pending');
      expect(component.invitedMembers()[0].status).toBe('Acknowledged');
    });

    it('renders empty pending list when there are no pending invitations', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('PendingInvitations');
      op.flush({
        data: { pendingInvitations: [] },
      });

      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 25));
      fixture.detectChanges();

      expect(component.showListing()).toBe(true);
      expect(component.pendingInvitations()).toHaveLength(0);
    });

    it('shows error state when GraphQL request fails', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('PendingInvitations');
      op.flush({
        errors: [{ message: 'FORBIDDEN: not allowed' }],
      });

      await fixture.whenStable();
      fixture.detectChanges();

      expect(component.showError()).toBe(true);
      expect(component.vm().state).toBe('error');
    });
  });

  // ── invite form ────────────────────────────────────────────────────────

  describe('invite form', () => {
    beforeEach(() => {
      localStorage.setItem(
        'indiaworks_session',
        JSON.stringify({
          organisationId: 'org-1',
          userId: 'user-1',
        }),
      );
    });

    afterEach(() => {
      localStorage.removeItem('indiaworks_session');
    });

    it('cannot open invite form without session', () => {
      localStorage.removeItem('indiaworks_session');
      component.vm.update((v) => ({
        ...v,
        selectedOrganisationId: null,
        callerUserAccountId: null,
      }));

      expect(component.canOpenInviteForm()).toBe(false);
    });

    it('opens invite form from listing', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('PendingInvitations');
      op.flush({
        data: { pendingInvitations: [] },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      component.onOpenInviteForm();
      fixture.detectChanges();

      expect(component.showInviteForm()).toBe(true);
      expect(component.vm().state).toBe('invite-form');
      expect(component.inviteeIdentifierTypeValue()).toBe('Email');
      expect(component.inviteeRoleValue()).toBe('Viewer');
    });

    it('cancel returns to listing', () => {
      component.vm.update((v) => ({
        ...v,
        state: 'invite-form',
        inviteeIdentifierValue: 'x',
      }));

      component.onCancelInviteForm();

      expect(component.showListing()).toBe(true);
      expect(component.vm().inviteeIdentifierValue).toBe('');
    });

    it('changing identifier type resets the value', () => {
      component.vm.update((v) => ({
        ...v,
        state: 'invite-form',
        inviteeIdentifierType: 'Email',
        inviteeIdentifierValue: 'old',
      }));

      component.onIdentifierTypeChange('Phone');

      expect(component.inviteeIdentifierTypeValue()).toBe('Phone');
      expect(component.inviteeIdentifierValue()).toBe('');
    });

    it('identifier value is trimmed and capped', () => {
      component.vm.update((v) => ({
        ...v,
        state: 'invite-form',
      }));

      component.onIdentifierValueInput('  +919876543210  ');

      expect(component.inviteeIdentifierValue()).toBe('+919876543210');
    });

    it('cannot submit invite without identifier', () => {
      component.vm.update((v) => ({
        ...v,
        state: 'invite-form',
        inviteeIdentifierType: 'Email',
        inviteeIdentifierValue: '',
      }));

      expect(component.canSubmitInvite()).toBe(false);
    });

    it('cannot submit invite while loading', () => {
      component.vm.update((v) => ({
        ...v,
        state: 'invite-form',
        inviteeIdentifierType: 'Email',
        inviteeIdentifierValue: 'a@b.com',
        inviteLoading: true,
      }));

      expect(component.canSubmitInvite()).toBe(false);
    });

    it('can submit invite with valid fields', () => {
      component.vm.update((v) => ({
        ...v,
        state: 'invite-form',
        selectedOrganisationId: 'org-1',
        callerUserAccountId: 'user-1',
        inviteeIdentifierType: 'Email',
        inviteeIdentifierValue: 'a@b.com',
        inviteLoading: false,
      }));

      expect(component.canSubmitInvite()).toBe(true);
    });
  });

  // ── invite submit ──────────────────────────────────────────────────────

  describe('invite submit', () => {
    beforeEach(() => {
      localStorage.setItem(
        'indiaworks_session',
        JSON.stringify({
          organisationId: 'org-1',
          userId: 'user-1',
        }),
      );
    });

    afterEach(() => {
      localStorage.removeItem('indiaworks_session');
    });

    it('sends invite mutation and shows success', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const listOp = apolloTesting.expectOne('PendingInvitations');
      listOp.flush({
        data: { pendingInvitations: [] },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      component.onOpenInviteForm();
      component.onIdentifierValueInput('member@example.com');
      component.onRoleChange('Recruiter');
      fixture.detectChanges();

      const submit = component.onInviteSubmit();

      expect(component.vm().inviteLoading).toBe(true);
      await new Promise((resolve) => setTimeout(resolve, 25));

      const inviteOp = apolloTesting.expectOne('InviteMember');
      inviteOp.flush({
        data: {
          inviteMember: {
            isSuccess: true,
            invitationId: 'inv-new',
            errorCode: null,
            errorMessage: null,
          },
        },
      });

      await submit;
      await fixture.whenStable();
      fixture.detectChanges();

      expect(component.showSuccess()).toBe(true);
      expect(component.vm().state).toBe('success');
      expect(component.vm().message).toBe('Invitation sent.');
    });

    it('shows error when invite mutation returns failure code', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const listOp = apolloTesting.expectOne('PendingInvitations');
      listOp.flush({
        data: { pendingInvitations: [] },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      component.onOpenInviteForm();
      component.onIdentifierValueInput('member@example.com');
      fixture.detectChanges();

      const submit = component.onInviteSubmit();

      await new Promise((resolve) => setTimeout(resolve, 25));
      const inviteOp = apolloTesting.expectOne('InviteMember');
      inviteOp.flush({
        data: {
          inviteMember: {
            isSuccess: false,
            invitationId: null,
            errorCode: 'ROLE_NOT_ALLOWED',
            errorMessage: 'Only Owners and Admins may invite members.',
          },
        },
      });

      await submit;
      await fixture.whenStable();
      fixture.detectChanges();

      expect(component.showError()).toBe(true);
      expect(component.vm().errorCode).toBe('ROLE_NOT_ALLOWED');
      expect(component.vm().message).toBe('Only Owners and Admins may perform this action.');
    });

    it('shows error when GraphQL request fails', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const listOp = apolloTesting.expectOne('PendingInvitations');
      listOp.flush({
        data: { pendingInvitations: [] },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      component.onOpenInviteForm();
      component.onIdentifierValueInput('member@example.com');
      fixture.detectChanges();

      const submit = component.onInviteSubmit();

      await new Promise((resolve) => setTimeout(resolve, 25));
      const inviteOp = apolloTesting.expectOne('InviteMember');
      inviteOp.flush({
        errors: [{ message: 'GRAPHQL_ERROR' }],
      });

      await submit;
      await fixture.whenStable();
      fixture.detectChanges();

      expect(component.showError()).toBe(true);
      expect(component.vm().errorCode).toBe('GRAPHQL_ERROR');
    });
  });

  // ── revoke ─────────────────────────────────────────────────────────────

  describe('revoke', () => {
    beforeEach(() => {
      localStorage.setItem(
        'indiaworks_session',
        JSON.stringify({
          organisationId: 'org-1',
          userId: 'user-1',
        }),
      );
    });

    afterEach(() => {
      localStorage.removeItem('indiaworks_session');
    });

    it('opens revoke confirmation for a pending invite', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('PendingInvitations');
      op.flush({
        data: {
          pendingInvitations: [
            {
              id: 'inv-1',
              organisationId: 'org-1',
              invitedByUserAccountId: 'user-1',
              inviteeIdentifierType: 'Email',
              role: 'Viewer',
              status: 'Pending',
              expiresAt: '2026-10-07T00:00:00Z',
              createdAt: '2026-09-30T00:00:00Z',
            },
          ],
        },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      component.onRequestRevoke(component.vm().invitations[0]);
      fixture.detectChanges();

      expect(component.vm().inviteIdToRevoke).toBe('inv-1');
    });

    it('cancel revoke hides confirmation', () => {
      component.vm.update((v) => ({
        ...v,
        inviteIdToRevoke: 'inv-1',
      }));

      component.onCancelRevoke();

      expect(component.vm().inviteIdToRevoke).toBeNull();
    });

    it('sends revoke mutation and clears invitation on success', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const listOp = apolloTesting.expectOne('PendingInvitations');
      listOp.flush({
        data: {
          pendingInvitations: [
            {
              id: 'inv-1',
              organisationId: 'org-1',
              invitedByUserAccountId: 'user-1',
              inviteeIdentifierType: 'Email',
              role: 'Viewer',
              status: 'Pending',
              expiresAt: '2026-10-07T00:00:00Z',
              createdAt: '2026-09-30T00:00:00Z',
            },
          ],
        },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      component.onRequestRevoke(component.vm().invitations[0]);
      fixture.detectChanges();

      const revoke = component.onConfirmRevoke();

      expect(component.vm().revokeLoading).toBe(true);

      const revokeOp = apolloTesting.expectOne('RevokeInvitation');
      revokeOp.flush({
        data: {
          revokeInvitation: {
            isSuccess: true,
            errorCode: null,
            errorMessage: null,
          },
        },
      });

      await revoke;
      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 0));
      fixture.detectChanges();

      expect(component.vm().revokeLoading).toBe(false);
      expect(component.vm().inviteIdToRevoke).toBeNull();
      expect(component.vm().message).toBe('Invitation revoked.');
    });

    it('shows error when revoke mutation fails', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const listOp = apolloTesting.expectOne('PendingInvitations');
      listOp.flush({
        data: {
          pendingInvitations: [
            {
              id: 'inv-1',
              organisationId: 'org-1',
              invitedByUserAccountId: 'user-1',
              inviteeIdentifierType: 'Email',
              role: 'Viewer',
              status: 'Pending',
              expiresAt: '2026-10-07T00:00:00Z',
              createdAt: '2026-09-30T00:00:00Z',
            },
          ],
        },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      component.onRequestRevoke(component.vm().invitations[0]);
      fixture.detectChanges();

      const revoke = component.onConfirmRevoke();

      const revokeOp = apolloTesting.expectOne('RevokeInvitation');
      revokeOp.flush({
        data: {
          revokeInvitation: {
            isSuccess: false,
            errorCode: 'INVITATION_NOT_FOUND',
            errorMessage: 'Invitation not found.',
          },
        },
      });

      await revoke;
      await fixture.whenStable();
      await new Promise((resolve) => setTimeout(resolve, 0));
      fixture.detectChanges();

      expect(component.showError()).toBe(true);
      expect(component.vm().errorCode).toBe('INVITATION_NOT_FOUND');
      expect(component.vm().inviteIdToRevoke).toBeNull();
    });
  });

  // ── responsive / render guard ──────────────────────────────────────────

  describe('render guard', () => {
    it('renders without console errors at component level', async () => {
      localStorage.setItem(
        'indiaworks_session',
        JSON.stringify({
          organisationId: 'org-1',
          userId: 'user-1',
        }),
      );

      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
      const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {});

      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('PendingInvitations');
      op.flush({
        data: { pendingInvitations: [] },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement).toBeTruthy();
      expect(consoleError).not.toHaveBeenCalled();
      expect(consoleWarn).not.toHaveBeenCalled();

      consoleError.mockRestore();
      consoleWarn.mockRestore();
      localStorage.removeItem('indiaworks_session');
    });
  });
});
