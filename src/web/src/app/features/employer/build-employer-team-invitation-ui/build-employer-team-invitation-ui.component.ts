import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { firstValueFrom, Subscription } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { TitleCasePipe } from '@angular/common';
import { Apollo } from 'apollo-angular';
import {
  PendingInvitationsDocument,
  PendingInvitationsQuery,
  PendingInvitationsQueryVariables,
  InviteMemberDocument,
  InviteMemberMutation,
  InviteMemberMutationVariables,
  InviteMemberInput,
  RevokeInvitationDocument,
  RevokeInvitationMutation,
  RevokeInvitationMutationVariables,
  RevokeInvitationInput,
} from '../../../core/graphql/generated/graphql';

type ViewState = 'loading' | 'listing' | 'invite-form' | 'success' | 'error';

type InviteeIdentifierType = 'Email' | 'Phone';

interface PendingInvitation {
  id: string;
  organisationId: string;
  invitedByUserAccountId: string;
  inviteeIdentifierType: string;
  role: string;
  status: string;
  expiresAt: unknown;
  createdAt: unknown;
}

interface ViewModel {
  state: ViewState;
  invitations: PendingInvitation[];
  selectedOrganisationId: string | null;
  callerUserAccountId: string | null;
  inviteeIdentifierType: InviteeIdentifierType | null;
  inviteeIdentifierValue: string;
  inviteeRole: string;
  inviteLoading: boolean;
  revokeLoading: boolean;
  inviteIdToRevoke: string | null;
  message: string | null;
  errorCode: string | null;
}

const ALLOWED_ROLES = [
  { value: 'Owner', label: 'Owner' },
  { value: 'Admin', label: 'Admin' },
  { value: 'Recruiter', label: 'Recruiter' },
  { value: 'Viewer', label: 'Viewer' },
  { value: 'BillingAdmin', label: 'Billing Admin' },
];

@Component({
  selector: 'iw-build-employer-team-invitation-ui',
  standalone: true,
  imports: [FormsModule, TitleCasePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './build-employer-team-invitation-ui.component.html',
  styleUrl: './build-employer-team-invitation-ui.component.scss',
})
export class BuildEmployerTeamInvitationUiComponent implements OnInit, OnDestroy {
  private readonly apollo = inject(Apollo);
  private pendingInvitationsSubscription: Subscription | null = null;

  readonly vm = signal<ViewModel>({
    state: 'loading',
    invitations: [],
    selectedOrganisationId: null,
    callerUserAccountId: null,
    inviteeIdentifierType: null,
    inviteeIdentifierValue: '',
    inviteeRole: 'Viewer',
    inviteLoading: false,
    revokeLoading: false,
    inviteIdToRevoke: null,
    message: null,
    errorCode: null,
  });

  readonly inviteeIdentifierTypeValue = () => this.vm().inviteeIdentifierType;
  readonly inviteeIdentifierValue = () => this.vm().inviteeIdentifierValue;
  readonly inviteeRoleValue = () => this.vm().inviteeRole;
  readonly showLoading = computed(() => this.vm().state === 'loading');
  readonly showListing = computed(() => this.vm().state === 'listing');
  readonly showInviteForm = computed(() => this.vm().state === 'invite-form');
  readonly showSuccess = computed(() => this.vm().state === 'success');
  readonly showError = computed(() => this.vm().state === 'error');
  readonly errorMessage = computed(() => this.vm().message);
  readonly errorCode = computed(() => this.vm().errorCode);
  readonly pendingInvitations = computed(() =>
    this.vm().invitations.filter((i) => i.status === 'Pending'),
  );
  readonly invitedMembers = computed(() =>
    this.vm().invitations.filter((i) => i.status !== 'Pending'),
  );
  readonly roleOptions = ALLOWED_ROLES;

  readonly canOpenInviteForm = computed(
    () =>
      this.vm().selectedOrganisationId != null &&
      this.vm().callerUserAccountId != null &&
      !this.vm().inviteLoading,
  );

  readonly canSubmitInvite = computed(() => {
    const v = this.vm();
    if (!v.inviteeIdentifierType || !v.inviteeIdentifierValue.trim()) return false;
    if (!v.selectedOrganisationId || !v.callerUserAccountId) return false;
    return v.inviteeIdentifierValue.trim().length >= 2 && !v.inviteLoading;
  });

  readonly revokeConfirmLabel = computed(() => {
    const id = this.vm().inviteIdToRevoke;
    const invitation = id != null ? this.vm().invitations.find((i) => i.id === id) : null;
    return invitation
      ? `Revoke invitation to ${invitation.inviteeIdentifierType.toLowerCase()}`
      : 'Revoke this invitation?';
  });

  ngOnInit(): void {
    this.loadInvitations();
  }

  ngOnDestroy(): void {
    if (this.pendingInvitationsSubscription) {
      this.pendingInvitationsSubscription.unsubscribe();
    }
  }

  private loadInvitations(): void {
    const orgId = this.readOrganisationId();
    const callerId = this.readCallerUserAccountId();

    if (!orgId || !callerId) {
      this.vm.update((v) => ({
        ...v,
        state: 'error',
        message: 'Session context missing. Please log in again.',
        errorCode: 'UNAUTHENTICATED',
      }));
      return;
    }

    this.vm.update((v) => ({
      ...v,
      selectedOrganisationId: orgId,
      callerUserAccountId: callerId,
      state: 'loading',
      invitations: [],
    }));

    this.apollo
      .watchQuery<PendingInvitationsQuery, PendingInvitationsQueryVariables>({
        query: PendingInvitationsDocument,
        variables: { organisationId: orgId, callerUserAccountId: callerId },
        fetchPolicy: 'network-only',
      })
      .valueChanges.subscribe({
        next: ({ data, loading, error }) => {
          if (error) {
            this.vm.update((v) => ({
              ...v,
              state: 'error',
              message: this.mapError(error),
              errorCode: this.extractErrorCode(error),
            }));
            return;
          }
          const invitations = (data?.pendingInvitations ?? []) as PendingInvitation[];
          this.vm.update((v) => ({
            ...v,
            state: loading ? 'loading' : 'listing',
            invitations,
          }));
        },
        error: () => {
          this.vm.update((v) => ({
            ...v,
            state: 'error',
            message: 'Unable to load invitations.',
            errorCode: 'GRAPHQL_ERROR',
          }));
        },
      });
  }

  private readOrganisationId(): string | null {
    try {
      const payload = localStorage.getItem('indiaworks_session');
      if (payload) {
        const parsed = JSON.parse(payload);
        return parsed.organisationId ?? parsed.organisation_id ?? null;
      }
    } catch {
      return null;
    }
    return null;
  }

  private readCallerUserAccountId(): string | null {
    try {
      const payload = localStorage.getItem('indiaworks_session');
      if (payload) {
        const parsed = JSON.parse(payload);
        return (
          parsed.userId ??
          parsed.user_id ??
          parsed.callerUserAccountId ??
          parsed.caller_user_account_id ??
          null
        );
      }
    } catch {
      return null;
    }
    return null;
  }

  onOpenInviteForm(): void {
    if (!this.canOpenInviteForm()) return;
    this.vm.update((v) => ({
      ...v,
      state: 'invite-form',
      inviteeIdentifierType: 'Email',
      inviteeIdentifierValue: '',
      inviteeRole: 'Viewer',
    }));
  }

  onCancelInviteForm(): void {
    this.vm.update((v) => ({
      ...v,
      state: 'listing',
      inviteeIdentifierValue: '',
    }));
  }

  onIdentifierTypeChange(value: string): void {
    this.vm.update((v) => ({
      ...v,
      inviteeIdentifierType: value as InviteeIdentifierType | null,
      inviteeIdentifierValue: '',
    }));
  }

  onIdentifierValueInput(value: string): void {
    this.vm.update((v) => ({
      ...v,
      inviteeIdentifierValue: value.trim().slice(0, 255),
    }));
  }

  onRoleChange(value: string): void {
    this.vm.update((v) => ({ ...v, inviteeRole: value }));
  }

  async onInviteSubmit(): Promise<void> {
    if (!this.canSubmitInvite()) return;

    const orgId = this.vm().selectedOrganisationId;
    const callerId = this.vm().callerUserAccountId;

    if (!orgId || !callerId) {
      this.vm.update((v) => ({
        ...v,
        state: 'error',
        message: 'Session context missing.',
        errorCode: 'UNAUTHENTICATED',
      }));
      return;
    }

    this.vm.update((v) => ({
      ...v,
      inviteLoading: true,
      message: null,
      errorCode: null,
    }));

    const identifierValue = this.vm().inviteeIdentifierValue;
    const inviteeHash = await this.hashString(identifierValue);
    const token = `${orgId}-${Date.now()}-${Math.random().toString(36).slice(2)}`;
    const tokenHash = await this.hashString(token);

    const input: InviteMemberInput = {
      organisationId: this.toGuid(orgId),
      invitedByUserAccountId: this.toGuid(callerId),
      inviteeIdentifierType: this.vm().inviteeIdentifierType ?? 'Email',
      inviteeIdentifierHash: inviteeHash,
      role: this.vm().inviteeRole,
      tokenHash,
      expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000),
    };

    try {
      const { data, error } = await firstValueFrom(
        this.apollo.mutate<InviteMemberMutation, InviteMemberMutationVariables>({
          mutation: InviteMemberDocument,
          variables: { input },
          fetchPolicy: 'network-only',
        }),
      );

      this.vm.update((v) => ({ ...v, inviteLoading: false }));
      if (error) {
        this.vm.update((v) => ({
          ...v,
          state: 'error',
          message: this.mapError(error),
          errorCode: this.extractErrorCode(error),
        }));
        return;
      }
      const result = data?.inviteMember;
      if (!result) {
        this.vm.update((v) => ({
          ...v,
          state: 'error',
          message: 'Unexpected response from server.',
          errorCode: 'UNEXPECTED_RESPONSE',
        }));
        return;
      }
      if (!result.isSuccess) {
        const errorCode = result.errorCode ?? 'UNKNOWN';
        this.vm.update((v) => ({
          ...v,
          state: 'error',
          message: this.mapError({ error: { code: errorCode } }),
          errorCode,
        }));
        return;
      }
      this.vm.update((v) => ({
        ...v,
        state: 'success',
        message: 'Invitation sent.',
        errorCode: null,
      }));
    } catch (error) {
      this.vm.update((v) => ({
        ...v,
        inviteLoading: false,
        state: 'error',
        message: this.mapError(error),
        errorCode: this.extractErrorCode(error),
      }));
    }
  }

  onViewInvite(invitation: PendingInvitation): void {
    this.vm.update((v) => ({
      ...v,
      inviteIdToRevoke: null,
    }));
  }

  onRequestRevoke(invitation: PendingInvitation): void {
    this.vm.update((v) => ({
      ...v,
      inviteIdToRevoke: invitation.id,
    }));
  }

  onCancelRevoke(): void {
    this.vm.update((v) => ({ ...v, inviteIdToRevoke: null }));
  }

  async onConfirmRevoke(): Promise<void> {
    const id = this.vm().inviteIdToRevoke;
    if (!id || !this.vm().selectedOrganisationId || !this.vm().callerUserAccountId) return;

    this.vm.update((v) => ({
      ...v,
      revokeLoading: true,
    }));

    const input: RevokeInvitationInput = {
      invitationId: this.toGuid(id),
      revokedByUserAccountId: this.toGuid(this.vm().callerUserAccountId),
    };

    try {
      const { data, error } = await firstValueFrom(
        this.apollo.mutate<RevokeInvitationMutation, RevokeInvitationMutationVariables>({
          mutation: RevokeInvitationDocument,
          variables: { input },
          fetchPolicy: 'network-only',
        }),
      );

      this.vm.update((v) => ({ ...v, revokeLoading: false, inviteIdToRevoke: null }));
      if (error) {
        this.vm.update((v) => ({
          ...v,
          state: 'error',
          message: this.mapError(error),
          errorCode: this.extractErrorCode(error),
        }));
        return;
      }
      const result = data?.revokeInvitation;
      if (!result || !result.isSuccess) {
        const errorCode = result?.errorCode ?? 'UNKNOWN';
        this.vm.update((v) => ({
          ...v,
          state: 'error',
          message: this.mapError({ error: { code: errorCode } }),
          errorCode,
        }));
        return;
      }
      this.vm.update((v) => ({
        ...v,
        message: 'Invitation revoked.',
        errorCode: null,
      }));
    } catch (error) {
      this.vm.update((v) => ({
        ...v,
        revokeLoading: false,
        inviteIdToRevoke: null,
        state: 'error',
        message: this.mapError(error),
        errorCode: this.extractErrorCode(error),
      }));
    }
  }

  onDismissError(): void {
    this.vm.update((v) => ({
      ...v,
      state: 'listing',
      message: null,
      errorCode: null,
    }));
  }

  private async hashString(value: string): Promise<Array<number>> {
    const msg = new TextEncoder().encode(value);
    const hash =
      typeof crypto !== 'undefined' && crypto.subtle
        ? await crypto.subtle.digest('SHA-256', msg)
        : null;
    if (!hash) {
      return Array.from({ length: 32 }, () => 0);
    }
    const bytes = new Uint8Array(hash);
    return Array.from(bytes.slice(0, 32));
  }

  formatCreatedAt(value: unknown): string {
    if (!value) return '';
    const date = new Date(value as string);
    if (isNaN(date.getTime())) return '';
    return date.toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
    });
  }

  private toGuid(value: string | null): unknown {
    return value;
  }

  private extractErrorCode(error: unknown): string | null {
    if (error && typeof error === 'object' && 'graphQLErrors' in error) {
      const gqlErrors = (error as { graphQLErrors?: Array<{ extensions?: { code?: string } }> })
        .graphQLErrors;
      if (gqlErrors && gqlErrors.length > 0) {
        return gqlErrors[0].extensions?.code ?? 'GRAPHQL_ERROR';
      }
    }
    if (error && typeof error === 'object' && 'error' in error) {
      const errObj = error as { error?: { Code?: string; code?: string } };
      return errObj.error?.Code ?? errObj.error?.code ?? 'UNKNOWN';
    }
    if (error && typeof error === 'object' && 'message' in error) {
      const message = String((error as { message?: unknown }).message ?? '');
      if (message.includes('GRAPHQL_ERROR')) {
        return 'GRAPHQL_ERROR';
      }
    }
    return 'UNKNOWN';
  }

  private mapError(error: unknown): string {
    const code = this.extractErrorCode(error);
    if (!code) {
      return 'An unexpected error occurred. Please try again.';
    }
    const messages: Record<string, string> = {
      VALIDATION_FAILED: 'Please check your input and try again.',
      FORBIDDEN: 'You do not have permission to perform this action.',
      UNAUTHENTICATED: 'Session expired. Please log in again.',
      ORGANISATION_NOT_FOUND: 'Organisation not found.',
      MEMBERSHIP_REQUIRED: 'Membership required.',
      ROLE_NOT_ALLOWED: 'Only Owners and Admins may perform this action.',
      INVITATION_NOT_FOUND: 'Invitation not found.',
      INVITATION_NOT_ACTIVE: 'This invitation is no longer active.',
      INVITATION_EXPIRED: 'This invitation has expired.',
      RATE_LIMITED: 'Too many requests. Please wait.',
      NETWORK_ERROR: 'Unable to connect to server. Please check your connection.',
      GRAPHQL_ERROR: 'An error occurred. Please try again.',
    };
    return messages[code] ?? 'An unexpected error occurred. Please try again.';
  }
}
