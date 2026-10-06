import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TitleCasePipe } from '@angular/common';
import { Router } from '@angular/router';
import { Apollo } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import {
  MyOrganisationsDocument,
  MyOrganisationsQuery,
  MyOrganisationsQueryVariables,
  CreateOrganisationDocument,
  CreateOrganisationMutation,
  CreateOrganisationMutationVariables,
  CreateOrganisationInput,
  EmployerSectorInput,
} from '../../../core/graphql/generated/graphql';

type ViewState = 'loading' | 'listing' | 'create-form' | 'success' | 'error';
type Section = 'overview' | 'company-details' | 'owner-confirmation';

interface OrganisationSummary {
  id: string;
  legalName: string;
  tradeName: string | null;
  sector: string | null;
  employeeBand: string | null;
  organisationType: string | null;
  nic2025Code: string | null;
  verificationStatus: string;
  createdAt: unknown;
  role: string;
}

interface ViewModel {
  state: ViewState;
  section: Section;
  organisations: OrganisationSummary[];
  selectedOrganisationId: string | null;
  legalName: string;
  tradeName: string;
  sector: EmployerSectorInput | null;
  employeeBand: string;
  organisationType: string;
  nic2025Code: string;
  message: string | null;
  errorCode: string | null;
  createLoading: boolean;
  listLoading: boolean;
  canSubmitLegalName: boolean;
  canSubmitCreate: boolean;
  createError: string | null;
}

const SECTORS: { value: EmployerSectorInput; label: string }[] = [
  { value: 'Private', label: 'Private Company' },
  { value: 'Government', label: 'Government' },
  { value: 'Psu', label: 'Public Sector Undertaking' },
  { value: 'Ngo', label: 'NGO / Non-Profit' },
  { value: 'Household', label: 'Household / Family Business' },
];

@Component({
  selector: 'iw-build-employer-organisation-setup-ui',
  standalone: true,
  imports: [FormsModule, TitleCasePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './build-employer-organisation-setup-ui.component.html',
  styleUrl: './build-employer-organisation-setup-ui.component.scss',
})
export class BuildEmployerOrganisationSetupUiComponent implements OnInit, OnDestroy {
  private readonly apollo = inject(Apollo);
  private readonly router = inject(Router);

  readonly vm = signal<ViewModel>({
    state: 'loading',
    section: 'overview',
    organisations: [],
    selectedOrganisationId: null,
    legalName: '',
    tradeName: '',
    sector: null,
    employeeBand: '',
    organisationType: '',
    nic2025Code: '',
    message: null,
    errorCode: null,
    createLoading: false,
    listLoading: true,
    canSubmitLegalName: false,
    canSubmitCreate: false,
    createError: null,
  });

  // Template-bound reads
  readonly legalNameValue = () => this.vm().legalName;
  readonly tradeNameValue = () => this.vm().tradeName;
  readonly employeeBandValue = () => this.vm().employeeBand;
  readonly organisationTypeValue = () => this.vm().organisationType;
  readonly nic2025CodeValue = () => this.vm().nic2025Code;

  readonly showLoading = computed(() => this.vm().state === 'loading');
  readonly showListing = computed(() => this.vm().state === 'listing');
  readonly showCreateForm = computed(() => this.vm().state === 'create-form');
  readonly showSuccess = computed(() => this.vm().state === 'success');
  readonly showError = computed(() => this.vm().state === 'error');
  readonly errorMessage = computed(() => this.vm().message);
  readonly errorCode = computed(() => this.vm().errorCode);

  readonly showOverview = computed(() => this.vm().section === 'overview');
  readonly showCompanyDetails = computed(() => this.vm().section === 'company-details');
  readonly showOwnerConfirmation = computed(() => this.vm().section === 'owner-confirmation');

  readonly selectedOrganisation = computed(
    () => this.vm().organisations.find((o) => o.id === this.vm().selectedOrganisationId) ?? null,
  );

  readonly sectorOptions = SECTORS;

  ngOnInit(): void {
    this.loadOrganisations();
  }

  ngOnDestroy(): void {
    // Cleanup if needed
  }

  // ── Organisation listing ──────────────────────────────────────────────

  private loadOrganisations(): void {
    this.vm.update((v) => ({ ...v, state: 'loading', listLoading: true, organisations: [] }));

    this.apollo
      .watchQuery<MyOrganisationsQuery, MyOrganisationsQueryVariables>({
        query: MyOrganisationsDocument,
        fetchPolicy: 'network-only',
      })
      .valueChanges.subscribe({
        next: ({ data, loading, error }) => {
          if (error) {
            this.vm.update((v) => ({
              ...v,
              state: 'error',
              listLoading: false,
              message: this.mapError(error),
              errorCode: this.extractErrorCode(error),
            }));
            return;
          }
          const orgs = (data?.myOrganisations ?? []) as OrganisationSummary[];
          this.vm.update((v) => ({
            ...v,
            state: loading ? 'loading' : 'listing',
            listLoading: loading,
            organisations: orgs,
          }));
        },
        error: () => {
          this.vm.update((v) => ({
            ...v,
            state: 'error',
            listLoading: false,
            message: 'Unable to load organisations.',
            errorCode: 'GRAPHQL_ERROR',
          }));
        },
      });
  }

  onSelectOrganisation(id: string | null): void {
    this.vm.update((v) => ({
      ...v,
      selectedOrganisationId: id,
    }));
  }

  onCreateNewClick(): void {
    this.vm.update((v) => ({
      ...v,
      state: 'create-form',
      section: 'overview',
      legalName: '',
      tradeName: '',
      sector: null,
      employeeBand: '',
      organisationType: '',
      nic2025Code: '',
      createError: null,
      canSubmitLegalName: false,
      canSubmitCreate: false,
    }));
  }

  onContinueWithSelection(): void {
    if (this.vm().selectedOrganisationId) {
      this.router.navigate(['/employer']);
    }
  }

  // ── Multi-step form navigation ────────────────────────────────────────

  onNextSection(): void {
    const current = this.vm().section;
    if (current === 'overview') {
      this.vm.update((v) => ({ ...v, section: 'company-details' }));
    } else if (current === 'company-details') {
      this.vm.update((v) => ({ ...v, section: 'owner-confirmation' }));
    }
  }

  onBackSection(): void {
    const current = this.vm().section;
    if (current === 'company-details') {
      this.vm.update((v) => ({ ...v, section: 'overview' }));
    } else if (current === 'owner-confirmation') {
      this.vm.update((v) => ({ ...v, section: 'company-details' }));
    }
  }

  // ── Form input handlers ───────────────────────────────────────────────

  onLegalNameInput(value: string): void {
    const trimmed = value.trim().slice(0, 200);
    this.vm.update((v) => ({
      ...v,
      legalName: trimmed,
      canSubmitLegalName: trimmed.length >= 2,
      canSubmitCreate: trimmed.length >= 2,
      createError: null,
    }));
  }

  onTradeNameInput(value: string): void {
    this.vm.update((v) => ({
      ...v,
      tradeName: value.trim().slice(0, 100),
    }));
  }

  onSectorChange(value: EmployerSectorInput | null): void {
    this.vm.update((v) => ({ ...v, sector: value }));
  }

  onEmployeeBandInput(value: string): void {
    this.vm.update((v) => ({
      ...v,
      employeeBand: value.trim().slice(0, 50),
    }));
  }

  onOrganisationTypeInput(value: string): void {
    this.vm.update((v) => ({
      ...v,
      organisationType: value.trim().slice(0, 50),
    }));
  }

  onNic2025CodeInput(value: string): void {
    this.vm.update((v) => ({
      ...v,
      nic2025Code: value.trim().slice(0, 20),
    }));
  }

  // ── Create organisation ───────────────────────────────────────────────

  async onCreateOrganisation(): Promise<void> {
    if (!this.vm().canSubmitCreate || this.vm().createLoading) {
      return;
    }

    this.vm.update((v) => ({
      ...v,
      createLoading: true,
      createError: null,
    }));

    const userId = this.extractUserId();
    if (!userId) {
      this.vm.update((v) => ({
        ...v,
        createLoading: false,
        state: 'error',
        message: 'Session invalid. Please log in again.',
        errorCode: 'UNAUTHENTICATED',
      }));
      return;
    }

    const input: CreateOrganisationInput = {
      creatorUserAccountId: userId,
      legalName: this.vm().legalName,
      tradeName: this.vm().tradeName || undefined,
      sector: this.vm().sector || undefined,
      employeeBand: this.vm().employeeBand || undefined,
      organisationType: this.vm().organisationType || undefined,
      nic2025Code: this.vm().nic2025Code || undefined,
    };

    try {
      const { data, error } = await firstValueFrom(
        this.apollo.mutate<CreateOrganisationMutation, CreateOrganisationMutationVariables>({
          mutation: CreateOrganisationDocument,
          variables: { input },
          fetchPolicy: 'network-only',
        }),
      );

      this.vm.update((v) => ({ ...v, createLoading: false }));

      if (error) {
        this.vm.update((v) => ({
          ...v,
          state: 'error',
          message: this.mapError(error),
          errorCode: this.extractErrorCode(error),
          createError: this.mapError(error),
        }));
        return;
      }

      const result = data?.createOrganisation;
      if (!result) {
        this.vm.update((v) => ({
          ...v,
          state: 'error',
          message: 'Unexpected response from server.',
          errorCode: 'UNEXPECTED_RESPONSE',
        }));
        return;
      }

      if (!result.isActive) {
        this.vm.update((v) => ({
          ...v,
          createError: result.errorMessage ?? 'Failed to create organisation.',
          errorCode: result.errorCode ?? 'UNKNOWN',
        }));
        return;
      }

      const currentLegalName = this.vm().legalName;
      this.vm.update((v) => ({
        ...v,
        state: 'success',
        section: 'owner-confirmation',
        message: `Organisation "${result.legalName ?? currentLegalName}" created successfully.`,
        errorCode: null,
        createError: null,
      }));
    } catch {
      this.vm.update((v) => ({
        ...v,
        createLoading: false,
        state: 'error',
        message: 'Network error. Please try again.',
        errorCode: 'NETWORK_ERROR',
      }));
    }
  }

  onCancelCreate(): void {
    this.vm.update((v) => ({
      ...v,
      state: 'listing',
      section: 'overview',
      legalName: '',
      tradeName: '',
      sector: null,
      employeeBand: '',
      organisationType: '',
      nic2025Code: '',
      createError: null,
      canSubmitLegalName: false,
      canSubmitCreate: false,
    }));
  }

  onRetryFromError(): void {
    if (this.vm().state === 'error') {
      this.vm.update((v) => ({
        ...v,
        state: 'listing',
        listLoading: true,
        message: null,
        errorCode: null,
      }));
      this.loadOrganisations();
    } else if (this.vm().section === 'owner-confirmation' && this.vm().createError) {
      this.vm.update((v) => ({
        ...v,
        createError: null,
        section: 'company-details',
      }));
    }
  }

  // ── Navigation after success ──────────────────────────────────────────

  onContinueToDashboard(): void {
    this.router.navigate(['/employer']);
  }

  // ── Private helpers ───────────────────────────────────────────────────

  private extractUserId(): string | null {
    // Try to get user ID from session storage or a service
    // For now, use a placeholder - in real app, inject AuthSessionStore
    try {
      const store = localStorage.getItem('indiaworks_session');
      if (store) {
        const session = JSON.parse(store);
        return session.userId || session.user_id || null;
      }
    } catch {
      // ignore
    }
    return null;
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
      ROLE_NOT_ALLOWED: 'This role is not allowed.',
      RATE_LIMITED: 'Too many requests. Please wait.',
      NETWORK_ERROR: 'Unable to connect to server. Please check your connection.',
      GRAPHQL_ERROR: 'An error occurred. Please try again.',
    };
    return messages[code] ?? 'An unexpected error occurred. Please try again.';
  }
}
