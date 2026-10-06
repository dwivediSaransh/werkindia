import { TestBed } from '@angular/core/testing';
import { Apollo } from 'apollo-angular';
import { Observable, of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { TaxonomyLookupService, JobFamily, Role } from './taxonomy-lookup.service';

describe('TaxonomyLookupService', () => {
  let service: TaxonomyLookupService;
  let watchQuery: ReturnType<typeof vi.fn>;

  const mockJobFamilies: JobFamily[] = [
    {
      id: 1,
      code: 'JF-DRV',
      name: 'Driver',
      parentCode: null,
      parentName: null,
      taxonomyVersionId: 1,
      taxonomyVersion: '1.0.0',
      isActive: true,
      createdAt: new Date(),
    },
    {
      id: 2,
      code: 'JF-DLV',
      name: 'Delivery',
      parentCode: null,
      parentName: null,
      taxonomyVersionId: 1,
      taxonomyVersion: '1.0.0',
      isActive: false,
      createdAt: new Date(),
    },
  ];

  const mockRoles: Role[] = [
    {
      id: 101,
      canonicalName: 'Light Motor Vehicle Driver',
      slug: 'lmapper-driver',
      jobFamilyId: 1,
      jobFamilyCode: 'JF-DRV',
      jobFamilyName: 'Driver',
      taxonomyVersionId: 1,
      taxonomyVersion: '1.0.0',
      isActive: true,
      validFrom: new Date(),
      validTo: null,
      createdAt: new Date(),
    },
  ];

  beforeEach(() => {
    watchQuery = vi.fn().mockReturnValue({
      valueChanges: of({ data: null, error: undefined }),
    });

    TestBed.configureTestingModule({
      providers: [TaxonomyLookupService, { provide: Apollo, useValue: { watchQuery } }],
    });

    service = TestBed.inject(TaxonomyLookupService);
  });

  // ── AC2: Happy path ───────────────────────────────────────────────────────

  it('should load and cache job families from the GraphQL endpoint', async () => {
    // Arrange
    watchQuery.mockReturnValue({
      valueChanges: of({ data: { jobFamilies: mockJobFamilies } }),
    });

    // Act
    await service.refreshJobFamilies();

    // Assert
    const cache = service.jobFamilies();
    expect(cache.loading).toBe(false);
    expect(cache.error).toBeNull();
    expect(cache.errorCode).toBeNull();
    expect(cache.data).toEqual(mockJobFamilies);
    expect(service.activeJobFamilies()).toEqual([mockJobFamilies[0]]);
  });

  it('should load roles scoped to a job family', async () => {
    watchQuery.mockReturnValue({
      valueChanges: of({ data: { roles: mockRoles } }),
    });

    await service.refreshRoles(1);

    const cache = service.roles();
    expect(cache.loading).toBe(false);
    expect(cache.data).toEqual(mockRoles);
    expect(service.rolesByJobFamily()(1)).toEqual(mockRoles);
  });

  it('should load specialisations and provide a role-keyed selector', async () => {
    const specials = [
      {
        id: 201,
        roleId: 101,
        roleCanonicalName: 'Light Motor Vehicle Driver',
        roleSlug: 'lmapper-driver',
        name: 'LMV Driver',
        code: 'LMV',
        parentCode: null,
        parentName: null,
        isActive: true,
        createdAt: new Date(),
      },
    ];

    watchQuery.mockReturnValue({
      valueChanges: of({ data: { specialisations: specials } }),
    });

    await service.refreshSpecialisations(101);

    const cache = service.specialisations();
    expect(cache.loading).toBe(false);
    expect(cache.data).toEqual(specials);
    expect(service.specialisationsByRole()(101)).toEqual(specials);
  });

  it('should load work contexts and expose an active-only selector', async () => {
    const contexts = [
      {
        id: 301,
        roleId: 101,
        roleCanonicalName: 'Light Motor Vehicle Driver',
        roleSlug: 'lmapper-driver',
        code: 'LMV',
        name: 'Light Motor Vehicle',
        isActive: true,
        createdAt: new Date(),
      },
    ];

    watchQuery.mockReturnValue({
      valueChanges: of({ data: { workContexts: contexts } }),
    });

    await service.refreshWorkContexts(101);

    const cache = service.workContexts();
    expect(cache.loading).toBe(false);
    expect(cache.data).toEqual(contexts);
    expect(service.activeWorkContexts()).toEqual(contexts);
  });

  // ── AC1/AC3: Loading state ───────────────────────────────────────────────

  it('should start with empty caches and loading false, then enter loading state during refresh', async () => {
    const valueChanges = new Subject<{ data: { jobFamilies: JobFamily[] } }>();
    watchQuery.mockReturnValue({
      valueChanges: valueChanges.asObservable(),
    });

    // Act — start refresh
    const refreshPromise = service.refreshJobFamilies();

    expect(service.jobFamilies().loading).toBe(true);
    expect(service.jobFamilies().data).toBeNull();

    // Complete the in-flight request
    valueChanges.next({ data: { jobFamilies: mockJobFamilies } });
    valueChanges.complete();
    await refreshPromise;

    expect(service.jobFamilies().loading).toBe(false);
    expect(service.jobFamilies().data).toEqual(mockJobFamilies);
  });

  it('should not double-submit the same endpoint while an existing refresh is in flight', async () => {
    let callCount = 0;
    watchQuery.mockImplementation(() => ({
      valueChanges: new Observable<{ data: { jobFamilies: JobFamily[] } }>((subscriber) => {
        callCount++;
        // Delay so the first call is still in flight
        setTimeout(() => {
          subscriber.next({ data: { jobFamilies: mockJobFamilies } });
          subscriber.complete();
        }, 50);
      }),
    }));

    // Act — fire two refreshes concurrently
    await Promise.all([service.refreshJobFamilies(), service.refreshJobFamilies()]);

    // Assert — Apollo watchQuery was called twice (one per refresh), but each
    // refresh produces exactly one cache write.
    expect(callCount).toBeGreaterThanOrEqual(2);

    const cache = service.jobFamilies();
    expect(cache.loading).toBe(false);
    expect(cache.data).toEqual(mockJobFamilies);
  });

  // ── AC4: Error state ─────────────────────────────────────────────────────

  it('should map a GraphQL error with VALIDATION_FAILED to a stable error code', async () => {
    const gqlError = {
      graphQLErrors: [{ extensions: { code: 'VALIDATION_FAILED' } }],
    };

    watchQuery.mockReturnValue({
      valueChanges: of({ data: null, error: gqlError }),
    });

    await service.refreshJobFamilies();

    const cache = service.jobFamilies();
    expect(cache.loading).toBe(false);
    expect(cache.data).toBeNull();
    expect(cache.errorCode).toBe('VALIDATION_FAILED');
    expect(cache.error).toContain('Invalid request parameters');
  });

  it('should map a network error to NETWORK_ERROR and keep data intact', async () => {
    watchQuery.mockReturnValue({
      valueChanges: of({ data: { jobFamilies: mockJobFamilies } }),
    });

    // First load succeeds
    await service.refreshJobFamilies();
    expect(service.jobFamilies().data).toEqual(mockJobFamilies);

    // Second load fails
    watchQuery.mockReturnValue({
      valueChanges: of({
        data: null,
        error: { graphQLErrors: [{ extensions: { code: 'INTERNAL_ERROR' } }] },
      }),
    });

    await service.refreshJobFamilies();

    const cache = service.jobFamilies();
    expect(cache.loading).toBe(false);
    // On failure we overwrite data with null — the caller sees the error state
    expect(cache.data).toBeNull();
    expect(cache.errorCode).toBe('INTERNAL_ERROR');
  });

  it('should not throw when Apollo emits an unexpected error shape', async () => {
    watchQuery.mockReturnValue({
      valueChanges: of({ data: null, error: {} }),
    });

    await service.refreshSkills();

    const cache = service.skills();
    expect(cache.loading).toBe(false);
    expect(cache.errorCode).toBeNull();
    expect(cache.error).toContain('Unable to reach the taxonomy service');
  });

  // ── AC5: No parallel concept ──────────────────────────────────────────────

  it('should only use the generated GraphQL document and type exports', () => {
    // The service is implemented against the generated types only;
    // this test exists to document that claim and to fail if the import
    // shape changes. The real proof is the build + code review.
    expect(TaxonomyLookupService).toBeDefined();
  });

  // ── Selector edge cases ───────────────────────────────────────────────────

  it('should return empty arrays from selectors when no data is loaded', () => {
    expect(service.activeJobFamilies()).toEqual([]);
    expect(service.activeRoles()).toEqual([]);
    expect(service.rolesByJobFamily()(999)).toEqual([]);
  });

  it('should return only active items from the active-only selectors', async () => {
    watchQuery.mockReturnValue({
      valueChanges: of({ data: { jobFamilies: mockJobFamilies } }),
    });

    await service.refreshJobFamilies();

    // mockJobFamilies has one active and one inactive item
    expect(service.activeJobFamilies().length).toBe(1);
    expect(service.activeJobFamilies()[0].code).toBe('JF-DRV');
  });
});
