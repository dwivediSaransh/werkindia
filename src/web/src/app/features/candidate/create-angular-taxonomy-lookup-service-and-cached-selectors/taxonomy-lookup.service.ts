import { Injectable, signal, computed, inject, WritableSignal } from '@angular/core';
import { Apollo } from 'apollo-angular';
import { firstValueFrom, Observable } from 'rxjs';
import {
  GeographyType as GeneratedGeographyType,
  JobFamiliesDocument,
  JobFamiliesQuery,
  JobFamiliesQueryVariables,
  RolesDocument,
  RolesQuery,
  RolesQueryVariables,
  SpecialisationsDocument,
  SpecialisationsQuery,
  SpecialisationsQueryVariables,
  WorkContextsDocument,
  WorkContextsQuery,
  WorkContextsQueryVariables,
  SkillsDocument,
  SkillsQuery,
  SkillsQueryVariables,
  TaxonomyVersionsDocument,
  TaxonomyVersionsQuery,
  TaxonomyVersionsQueryVariables,
  GeographiesDocument,
  GeographiesQuery,
  GeographiesQueryVariables,
  GeographyHierarchyDocument,
  GeographyHierarchyQuery,
  GeographyHierarchyQueryVariables,
} from '../../../core/graphql/generated/graphql';

// Generated GraphQL types are the source of truth; these aliases exist only to
// keep the public service API readable for components and tests.
export type GeographyType = GeneratedGeographyType;
export type JobFamily = JobFamiliesQuery['jobFamilies'][number];
export type Role = RolesQuery['roles'][number];
export type Specialisation = SpecialisationsQuery['specialisations'][number];
export type WorkContext = WorkContextsQuery['workContexts'][number];
export type Skill = SkillsQuery['skills'][number];
export type TaxonomyVersion = TaxonomyVersionsQuery['taxonomyVersions'][number];
export type Geography = GeographiesQuery['geographies'][number];

// ── Cache slot for one endpoint ──

interface TaxonomyCacheSlot<T> {
  data: T[] | null;
  loading: boolean;
  error: string | null;
  errorCode: string | null;
  stale: boolean;
}

interface TaxonomyQueryResult<TData> {
  data?: TData | null;
  error?: unknown;
  errors?: readonly unknown[];
}

// ── Service ──────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class TaxonomyLookupService {
  private readonly apollo = inject(Apollo);

  // Raw caches
  readonly jobFamilies = signal<TaxonomyCacheSlot<JobFamily>>({
    data: null,
    loading: false,
    error: null,
    errorCode: null,
    stale: true,
  });

  readonly roles = signal<TaxonomyCacheSlot<Role>>({
    data: null,
    loading: false,
    error: null,
    errorCode: null,
    stale: true,
  });

  readonly specialisations = signal<TaxonomyCacheSlot<Specialisation>>({
    data: null,
    loading: false,
    error: null,
    errorCode: null,
    stale: true,
  });

  readonly workContexts = signal<TaxonomyCacheSlot<WorkContext>>({
    data: null,
    loading: false,
    error: null,
    errorCode: null,
    stale: true,
  });

  readonly skills = signal<TaxonomyCacheSlot<Skill>>({
    data: null,
    loading: false,
    error: null,
    errorCode: null,
    stale: true,
  });

  readonly taxonomyVersions = signal<TaxonomyCacheSlot<TaxonomyVersion>>({
    data: null,
    loading: false,
    error: null,
    errorCode: null,
    stale: true,
  });

  readonly geographies = signal<TaxonomyCacheSlot<Geography>>({
    data: null,
    loading: false,
    error: null,
    errorCode: null,
    stale: true,
  });

  // ── Computed selectors (cached derivations) ───────────────────────────────

  readonly activeJobFamilies = computed(() =>
    (this.jobFamilies().data ?? []).filter((f) => f.isActive),
  );

  readonly activeRoles = computed(() => (this.roles().data ?? []).filter((r) => r.isActive));

  readonly activeSpecialisations = computed(() =>
    (this.specialisations().data ?? []).filter((s) => s.isActive),
  );

  readonly activeWorkContexts = computed(() =>
    (this.workContexts().data ?? []).filter((w) => w.isActive),
  );

  readonly activeSkills = computed(() => (this.skills().data ?? []).filter((s) => s.isActive));

  readonly rolesByJobFamily = computed(() => {
    const roles = this.roles().data ?? [];
    return (jobFamilyId: number) => roles.filter((r) => r.jobFamilyId === jobFamilyId);
  });

  readonly specialisationsByRole = computed(() => {
    const items = this.specialisations().data ?? [];
    return (roleId: number) => items.filter((s) => s.roleId === roleId);
  });

  readonly workContextsByRole = computed(() => {
    const items = this.workContexts().data ?? [];
    return (roleId: number) => items.filter((w) => w.roleId === roleId);
  });

  readonly geographiesByType = computed(() => {
    const items = this.geographies().data ?? [];
    return (type: GeographyType) => items.filter((g) => g.type === type);
  });

  readonly errorCodes = computed(() => {
    const codes: string[] = [];
    const slots: TaxonomyCacheSlot<unknown>[] = [
      this.jobFamilies(),
      this.roles(),
      this.specialisations(),
      this.workContexts(),
      this.skills(),
      this.taxonomyVersions(),
      this.geographies(),
    ];
    for (const slot of slots) {
      if (slot.errorCode) {
        codes.push(slot.errorCode);
      }
    }
    return codes;
  });

  // ── Fetch methods ─────────────────────────────────────────────────────────

  async refreshJobFamilies(): Promise<void> {
    await this.doFetch(
      this.jobFamilies,
      this.apollo.watchQuery<JobFamiliesQuery, JobFamiliesQueryVariables>({
        query: JobFamiliesDocument,
        fetchPolicy: 'network-only',
      }),
      (data) => data?.jobFamilies ?? [],
    );
  }

  async refreshRoles(jobFamilyId?: number): Promise<void> {
    const variables: RolesQueryVariables = jobFamilyId != null ? { jobFamilyId } : {};
    await this.doFetch(
      this.roles,
      this.apollo.watchQuery<RolesQuery, RolesQueryVariables>({
        query: RolesDocument,
        variables,
        fetchPolicy: 'network-only',
      }),
      (data) => data?.roles ?? [],
    );
  }

  async refreshSpecialisations(roleId?: number): Promise<void> {
    const variables: SpecialisationsQueryVariables = roleId != null ? { roleId } : {};
    await this.doFetch(
      this.specialisations,
      this.apollo.watchQuery<SpecialisationsQuery, SpecialisationsQueryVariables>({
        query: SpecialisationsDocument,
        variables,
        fetchPolicy: 'network-only',
      }),
      (data) => data?.specialisations ?? [],
    );
  }

  async refreshWorkContexts(roleId?: number): Promise<void> {
    const variables: WorkContextsQueryVariables = roleId != null ? { roleId } : {};
    await this.doFetch(
      this.workContexts,
      this.apollo.watchQuery<WorkContextsQuery, WorkContextsQueryVariables>({
        query: WorkContextsDocument,
        variables,
        fetchPolicy: 'network-only',
      }),
      (data) => data?.workContexts ?? [],
    );
  }

  async refreshSkills(): Promise<void> {
    await this.doFetch(
      this.skills,
      this.apollo.watchQuery<SkillsQuery, SkillsQueryVariables>({
        query: SkillsDocument,
        fetchPolicy: 'network-only',
      }),
      (data) => data?.skills ?? [],
    );
  }

  async refreshTaxonomyVersions(): Promise<void> {
    await this.doFetch(
      this.taxonomyVersions,
      this.apollo.watchQuery<TaxonomyVersionsQuery, TaxonomyVersionsQueryVariables>({
        query: TaxonomyVersionsDocument,
        fetchPolicy: 'network-only',
      }),
      (data) => data?.taxonomyVersions ?? [],
    );
  }

  async refreshGeographies(type?: GeographyType, search?: string): Promise<void> {
    const variables: GeographiesQueryVariables = {};
    if (type != null) {
      variables.type = type;
    }
    if (search != null) {
      variables.search = search;
    }
    await this.doFetch(
      this.geographies,
      this.apollo.watchQuery<GeographiesQuery, GeographiesQueryVariables>({
        query: GeographiesDocument,
        variables,
        fetchPolicy: 'network-only',
      }),
      (data) => data?.geographies ?? [],
    );
  }

  async refreshGeographyHierarchy(type?: GeographyType, search?: string): Promise<void> {
    const variables: GeographyHierarchyQueryVariables = {};
    if (type != null) {
      variables.type = type;
    }
    if (search != null) {
      variables.search = search;
    }
    // geographyHierarchy returns the same Geography[] shape but uses a different field name
    await this.doFetch(
      this.geographies,
      this.apollo.watchQuery<GeographyHierarchyQuery, GeographyHierarchyQueryVariables>({
        query: GeographyHierarchyDocument,
        variables,
        fetchPolicy: 'network-only',
      }),
      (data) => {
        const items: Geography[] = [];
        for (const g of data?.geographyHierarchy ?? []) {
          items.push({
            id: g.id as string,
            type: g.type as GeographyType,
            code: g.code as string | null,
            name: g.name as string,
            parentId: g.parentId as string | null,
            parentCode: g.parentCode as string | null,
            parentName: g.parentName as string | null,
            latitude: g.latitude as number | null,
            longitude: g.longitude as number | null,
            createdAt: g.createdAt,
          });
        }
        return items;
      },
    );
  }

  private async doFetch<T, TData>(
    cache: WritableSignal<TaxonomyCacheSlot<T>>,
    queryRef: { valueChanges: Observable<TaxonomyQueryResult<TData>> },
    extract: (data: TData | null | undefined) => T[],
  ): Promise<void> {
    cache.update((c) => ({ ...c, loading: true, error: null, errorCode: null }));

    try {
      const result = await firstValueFrom(queryRef.valueChanges);
      const resultErrors = result.errors ?? this.extractGraphQLErrors(result.error);
      if (result.error || (resultErrors && resultErrors.length > 0)) {
        const sourceError = result.error ?? resultErrors?.[0];
        const gqlError = resultErrors?.[0] as { extensions?: { code?: string } } | undefined;
        const code = gqlError?.extensions?.code ?? null;
        cache.update((c) => ({
          ...c,
          loading: false,
          data: null,
          error: this.mapErrorMessage(sourceError),
          errorCode: code,
          stale: false,
        }));
        return;
      }
      const items = extract(result.data);
      cache.update((c) => ({
        ...c,
        data: items,
        loading: false,
        error: null,
        errorCode: null,
        stale: false,
      }));
    } catch (err) {
      const code = this.extractErrorCode(err);
      cache.update((c) => ({
        ...c,
        loading: false,
        error: this.mapErrorMessage(err),
        errorCode: code,
        stale: false,
      }));
    }
  }

  private extractErrorCode(error: unknown): string | null {
    if (error && typeof error === 'object' && 'graphQLErrors' in error) {
      const gqlErrors = (error as { graphQLErrors?: Array<{ extensions?: { code?: string } }> })
        .graphQLErrors;
      if (gqlErrors && gqlErrors.length > 0) {
        return gqlErrors[0].extensions?.code ?? null;
      }
    }
    if (error && typeof error === 'object' && 'error' in error) {
      const errObj = error as { error?: { Code?: string; code?: string } };
      return errObj.error?.Code ?? errObj.error?.code ?? null;
    }
    return null;
  }

  private extractGraphQLErrors(error: unknown): readonly unknown[] | null {
    if (error && typeof error === 'object' && 'graphQLErrors' in error) {
      const gqlErrors = (error as { graphQLErrors?: readonly unknown[] }).graphQLErrors;
      return gqlErrors && gqlErrors.length > 0 ? gqlErrors : null;
    }
    return null;
  }

  private mapErrorMessage(error: unknown): string {
    const code = this.extractErrorCode(error);
    if (!code) {
      return 'Unable to reach the taxonomy service.';
    }
    const messages: Record<string, string> = {
      VALIDATION_FAILED: 'Invalid request parameters.',
      FORBIDDEN: 'You do not have permission to view this taxonomy data.',
      UNAUTHENTICATED: 'Session expired. Please log in again.',
      NETWORK_ERROR: 'Unable to connect to the taxonomy service.',
    };
    return messages[code] ?? 'Unable to reach the taxonomy service.';
  }
}
