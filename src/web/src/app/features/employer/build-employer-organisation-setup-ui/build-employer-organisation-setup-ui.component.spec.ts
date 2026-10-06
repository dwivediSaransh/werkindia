import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Router } from '@angular/router';
import { ApolloTestingModule, ApolloTestingController } from 'apollo-angular/testing';
import { BuildEmployerOrganisationSetupUiComponent } from './build-employer-organisation-setup-ui.component';
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

describe('BuildEmployerOrganisationSetupUiComponent', () => {
  let fixture: ComponentFixture<BuildEmployerOrganisationSetupUiComponent>;
  let component: BuildEmployerOrganisationSetupUiComponent;
  let apolloTesting: ApolloTestingController;
  let router: Router;
  let originalConsoleError: typeof console.error;
  let originalConsoleLog: typeof console.log;

  // Reset TestBed and re-attach storage before each test configures it.
  beforeEach(() => {
    try {
      TestBed.resetTestingModule();
    } catch {
      /* ignore */
    }
    if (typeof globalThis === 'object') {
      globalThis.localStorage = workerStorage;
      globalThis.sessionStorage = workerStorage;
    }
    if (typeof global === 'object') {
      (global as any).localStorage = workerStorage;
      (global as any).sessionStorage = workerStorage;
    }
  });

  // Per-test isolated storage is supplied by test.process-setup.ts.
  // Reset TestBed and clear worker storage after each spec.
  afterEach(() => {
    try {
      TestBed.resetTestingModule();
    } catch {
      /* ignore */
    }
  });

  beforeEach(async () => {
    originalConsoleError = console.error;
    originalConsoleLog = console.log;
    console.error = () => {};
    console.log = () => {};

    await TestBed.configureTestingModule({
      imports: [
        ApolloTestingModule,
        BuildEmployerOrganisationSetupUiComponent,
        FormsModule,
        HttpClientTestingModule,
        RouterTestingModule,
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    fixture = TestBed.createComponent(BuildEmployerOrganisationSetupUiComponent);
    component = fixture.componentInstance;
    apolloTesting = TestBed.inject(ApolloTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    console.error = originalConsoleError;
    console.log = originalConsoleLog;
    apolloTesting.verify();
    try {
      TestBed.resetTestingModule();
    } catch {
      /* ignore */
    }
    if (typeof workerStore !== 'undefined') {
      workerStore.clear();
    }
  });

  // ── initial state ──────────────────────────────────────────────────────

  describe('initial state', () => {
    it('shows loading state initially', async () => {
      expect(component.showLoading()).toBe(true);
      expect(component.showListing()).toBe(false);
      expect(component.vm().state).toBe('loading');
      expect(component.vm().listLoading).toBe(true);
    });
  });

  // ── organisation listing ───────────────────────────────────────────────

  describe('organisation listing', () => {
    it('loads organisations and shows listing state', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('MyOrganisations');
      op.flush({
        data: {
          myOrganisations: [
            {
              id: 'org-1',
              legalName: 'Acme Corp',
              tradeName: 'Acme',
              sector: 'Private',
              employeeBand: '50-200',
              organisationType: 'Pvt Ltd',
              nic2025Code: null,
              verificationStatus: 'Verified',
              createdAt: '2024-01-01T00:00:00Z',
              role: 'Owner',
            },
          ],
        },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      expect(component.showListing()).toBe(true);
      expect(component.showLoading()).toBe(false);
      expect(component.vm().organisations).toHaveLength(1);
      expect(component.vm().organisations[0].legalName).toBe('Acme Corp');
    });

    it('shows empty state when no organisations exist', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('MyOrganisations');
      op.flush({
        data: { myOrganisations: [] },
      });

      await fixture.whenStable();
      fixture.detectChanges();

      expect(component.showListing()).toBe(true);
      expect(component.vm().organisations).toHaveLength(0);
    });

    it('handles listing error gracefully', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('MyOrganisations');
      op.flush({
        errors: [{ message: 'GRAPHQL_ERROR: something went wrong' }],
      });

      await fixture.whenStable();
      fixture.detectChanges();

      expect(component.showError()).toBe(true);
      expect(component.vm().state).toBe('error');
    });
  });

  // ── select organisation ────────────────────────────────────────────────

  describe('select organisation', () => {
    beforeEach(async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('MyOrganisations');
      op.flush({
        data: {
          myOrganisations: [
            {
              id: 'org-1',
              legalName: 'Acme Corp',
              tradeName: null,
              sector: 'Private',
              employeeBand: null,
              organisationType: null,
              nic2025Code: null,
              verificationStatus: 'Verified',
              createdAt: '2024-01-01T00:00:00Z',
              role: 'Owner',
            },
          ],
        },
      });
      await fixture.whenStable();
      fixture.detectChanges();
    });

    it('selects an organisation', () => {
      const orgCard = fixture.nativeElement.querySelector('.org-card');
      orgCard.click();
      fixture.detectChanges();

      expect(component.vm().selectedOrganisationId).toBe('org-1');
    });

    it('clears selection when clicking again', () => {
      component.onSelectOrganisation('org-1');
      fixture.detectChanges();

      expect(component.vm().selectedOrganisationId).toBe('org-1');

      component.onSelectOrganisation(null);
      fixture.detectChanges();

      expect(component.vm().selectedOrganisationId).toBeNull();
    });
  });

  // ── create new organisation flow ───────────────────────────────────────

  describe('create new organisation flow', () => {
    beforeEach(async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('MyOrganisations');
      op.flush({
        data: { myOrganisations: [] },
      });
      await fixture.whenStable();
      fixture.detectChanges();
    });

    it('navigates to create form when clicking create button', () => {
      const createBtn = fixture.nativeElement.querySelector('.btn-primary');
      createBtn.click();
      fixture.detectChanges();

      expect(component.showCreateForm()).toBe(true);
      expect(component.vm().state).toBe('create-form');
      expect(component.vm().section).toBe('overview');
      expect(component.vm().legalName).toBe('');
      expect(component.vm().canSubmitLegalName).toBe(false);
    });

    it('shows basic info step first', () => {
      component.onCreateNewClick();
      fixture.detectChanges();

      expect(component.showOverview()).toBe(true);
      expect(component.showCompanyDetails()).toBe(false);
      expect(component.showOwnerConfirmation()).toBe(false);
    });
  });

  // ── multi-step form navigation ─────────────────────────────────────────

  describe('multi-step form navigation', () => {
    beforeEach(() => {
      component.onCreateNewClick();
      component.onLegalNameInput('Acme Corporation');
    });

    it('advances from overview to company details', () => {
      component.onNextSection();

      expect(component.showCompanyDetails()).toBe(true);
      expect(component.vm().section).toBe('company-details');
    });

    it('goes back from company details to overview', () => {
      component.onNextSection();

      component.onBackSection();

      expect(component.showOverview()).toBe(true);
      expect(component.vm().section).toBe('overview');
    });

    it('advances from company details to owner confirmation', () => {
      component.onNextSection();

      component.onNextSection();

      expect(component.showOwnerConfirmation()).toBe(true);
      expect(component.vm().section).toBe('owner-confirmation');
    });

    it('goes back from owner confirmation to company details', () => {
      component.onNextSection();
      component.onNextSection();

      component.onBackSection();

      expect(component.showCompanyDetails()).toBe(true);
      expect(component.vm().section).toBe('company-details');
    });
  });

  // ── form validation ────────────────────────────────────────────────────

  describe('form validation', () => {
    beforeEach(() => {
      component.onCreateNewClick();
    });

    it('rejects legal name shorter than 2 characters', () => {
      component.onLegalNameInput('A');

      expect(component.vm().canSubmitLegalName).toBe(false);
    });

    it('accepts legal name with 2 or more characters', () => {
      component.onLegalNameInput('AC');

      expect(component.vm().canSubmitLegalName).toBe(true);
      expect(component.vm().canSubmitCreate).toBe(true);
    });

    it('accepts full legal name', () => {
      component.onLegalNameInput('Acme Corporation Pvt Ltd');

      expect(component.vm().canSubmitLegalName).toBe(true);
      expect(component.vm().legalName).toBe('Acme Corporation Pvt Ltd');
    });

    it('trims and limits legal name length', () => {
      component.onLegalNameInput('  Acme  ');

      expect(component.vm().legalName).toBe('Acme');
    });

    it('clears error when user types', () => {
      component.onLegalNameInput('A');

      expect(component.vm().createError).toBeNull();
    });
  });

  // ── sector selection ───────────────────────────────────────────────────

  describe('sector selection', () => {
    beforeEach(() => {
      component.onCreateNewClick();
    });

    it('updates sector on selection', () => {
      component.onSectorChange('Private');

      expect(component.vm().sector).toBe('Private');
    });

    it('handles null sector', () => {
      component.onSectorChange(null);

      expect(component.vm().sector).toBeNull();
    });
  });

  // ── create organisation mutation ───────────────────────────────────────

  describe('create organisation mutation', () => {
    beforeEach(async () => {
      localStorage.setItem(
        'indiaworks_session',
        JSON.stringify({
          userId: 'user-1',
        }),
      );
      component.onCreateNewClick();
      component.onLegalNameInput('Acme Corporation');
      component.onNextSection();

      component.onEmployeeBandInput('50-200');
      component.onOrganisationTypeInput('Pvt Ltd');
      component.onNextSection();
    });

    it('creates organisation successfully', async () => {
      const create = component.onCreateOrganisation();

      expect(component.vm().createLoading).toBe(true);

      const op = apolloTesting.expectOne('CreateOrganisation');
      op.flush({
        data: {
          createOrganisation: {
            isActive: true,
            organisationId: 'new-org-123',
            legalName: 'Acme Corporation',
            tradeName: null,
            sector: 'Private',
            employeeBand: '50-200',
            role: 'Owner',
            createdAt: '2024-03-01T00:00:00Z',
            errorCode: null,
            errorMessage: null,
          },
        },
      });

      await create;

      expect(component.showSuccess()).toBe(true);
      expect(component.vm().state).toBe('success');
      expect(component.errorMessage()).toContain('Acme Corporation');
    });

    it('handles create error with stable error code', async () => {
      const create = component.onCreateOrganisation();

      expect(component.vm().createLoading).toBe(true);

      const op = apolloTesting.expectOne('CreateOrganisation');
      op.flush({
        data: {
          createOrganisation: {
            isActive: false,
            organisationId: null,
            legalName: null,
            tradeName: null,
            sector: null,
            employeeBand: null,
            role: null,
            createdAt: null,
            errorCode: 'VALIDATION_FAILED',
            errorMessage: 'Legal name is required',
          },
        },
      });

      await create;

      expect(component.vm().createError).toBe('Legal name is required');
    });

    it('handles network error during create', async () => {
      const create = component.onCreateOrganisation();

      expect(component.vm().createLoading).toBe(true);

      const op = apolloTesting.expectOne('CreateOrganisation');
      op.flush({
        errors: [{ message: 'Network error' }],
      });

      await create;

      expect(component.showError()).toBe(true);
    });
  });

  // ── cancel and retry ───────────────────────────────────────────────────

  describe('cancel and retry', () => {
    it('cancels create and returns to listing', () => {
      component.onCreateNewClick();
      component.onLegalNameInput('Acme');

      component.onCancelCreate();

      expect(component.showListing()).toBe(true);
      expect(component.vm().state).toBe('listing');
      expect(component.vm().legalName).toBe('');
    });

    it('retries from error state', async () => {
      component.vm.update((v) => ({
        ...v,
        state: 'error',
        listLoading: false,
        message: 'Unable to load organisations.',
        errorCode: 'GRAPHQL_ERROR',
      }));

      component.onRetryFromError();

      expect(component.vm().state).toBe('loading');
      expect(component.vm().listLoading).toBe(true);

      const op = apolloTesting.expectOne('MyOrganisations');
      op.flush({
        data: { myOrganisations: [] },
      });
    });
  });

  // ── navigation after success ───────────────────────────────────────────

  describe('navigation after success', () => {
    beforeEach(async () => {
      localStorage.setItem(
        'indiaworks_session',
        JSON.stringify({
          userId: 'user-1',
        }),
      );
      component.onCreateNewClick();
      component.onLegalNameInput('Acme Corp');
      component.onNextSection();
      component.onNextSection();

      const create = component.onCreateOrganisation();

      const op = apolloTesting.expectOne('CreateOrganisation');
      op.flush({
        data: {
          createOrganisation: {
            isActive: true,
            organisationId: 'org-123',
            legalName: 'Acme Corp',
            tradeName: null,
            sector: 'Private',
            employeeBand: null,
            role: 'Owner',
            createdAt: '2024-01-01T00:00:00Z',
            errorCode: null,
            errorMessage: null,
          },
        },
      });

      await create;
    });

    it('navigates to employer dashboard on continue', () => {
      component.onContinueToDashboard();

      expect(router.navigate).toHaveBeenCalledWith(['/employer']);
    });
  });

  // ── responsive behavior ────────────────────────────────────────────────

  describe('responsive behavior', () => {
    it('renders without errors at component level', async () => {
      fixture.detectChanges();
      await fixture.whenStable();

      const op = apolloTesting.expectOne('MyOrganisations');
      op.flush({
        data: { myOrganisations: [] },
      });
      await fixture.whenStable();
      fixture.detectChanges();

      expect(fixture.nativeElement).toBeTruthy();
    });
  });
});
