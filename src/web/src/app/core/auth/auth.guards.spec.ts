import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { authGuard, personaGuard } from './auth.guards';
import { AuthSessionStore } from './auth-session.store';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

const mockRoute = {} as ActivatedRouteSnapshot;

function mockState(url: string): RouterStateSnapshot {
  return { url } as RouterStateSnapshot;
}

function runGuard(guard: ReturnType<typeof personaGuard> | typeof authGuard) {
  return TestBed.runInInjectionContext(() => guard(mockRoute, mockState('/test')));
}

function runAuthGuard(url: string) {
  return TestBed.runInInjectionContext(() => authGuard(mockRoute, mockState(url)));
}

describe('authGuard', () => {
  let store: AuthSessionStore;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
    });
    store = TestBed.inject(AuthSessionStore);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'createUrlTree').mockReturnValue({} as UrlTree);
  });

  it('allows navigation when session is authenticated', () => {
    store.setSession({
      accessToken: 'tok',
      refreshTokenHash: 'h',
      userId: 'u1',
      deviceId: 'd1',
      sessionId: 's1',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3_600_000),
    });
    const result = runAuthGuard('/candidate/jobs');
    expect(result).toBe(true);
  });

  it('redirects unauthenticated candidate route to /candidate/otp-login with returnUrl', () => {
    runAuthGuard('/candidate/jobs');
    expect(router.createUrlTree).toHaveBeenCalledWith(
      ['/candidate/otp-login'],
      expect.objectContaining({ queryParams: { returnUrl: '/candidate/jobs' } }),
    );
  });

  it('redirects unauthenticated employer route to /employer/otp-login', () => {
    runAuthGuard('/employer/dashboard');
    expect(router.createUrlTree).toHaveBeenCalledWith(
      ['/employer/otp-login'],
      expect.objectContaining({ queryParams: { returnUrl: '/employer/dashboard' } }),
    );
  });

  it('redirects unauthenticated admin route to /candidate/otp-login', () => {
    runAuthGuard('/admin/users');
    expect(router.createUrlTree).toHaveBeenCalledWith(
      ['/candidate/otp-login'],
      expect.objectContaining({ queryParams: { returnUrl: '/admin/users' } }),
    );
  });
});

describe('personaGuard', () => {
  let store: AuthSessionStore;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RouterTestingModule],
    });
    store = TestBed.inject(AuthSessionStore);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'createUrlTree').mockReturnValue({} as UrlTree);
  });

  it('allows navigation when persona matches expected', () => {
    store.setSession({
      accessToken: 'tok',
      refreshTokenHash: 'h',
      userId: 'u1',
      deviceId: 'd1',
      sessionId: 's1',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3_600_000),
    });
    const guard = personaGuard('Candidate');
    const result = runGuard(guard);
    expect(result).toBe(true);
  });

  it('allows Employer persona through employer guard', () => {
    store.setSession({
      accessToken: 'tok',
      refreshTokenHash: 'h',
      userId: 'u2',
      deviceId: 'd2',
      sessionId: 's2',
      persona: 'Employer',
      expiresOn: new Date(Date.now() + 3_600_000),
    });
    const guard = personaGuard('Employer');
    const result = runGuard(guard);
    expect(result).toBe(true);
  });

  it('redirects Candidate to /candidate/jobs when accessing employer routes', () => {
    store.setSession({
      accessToken: 'tok',
      refreshTokenHash: 'h',
      userId: 'u3',
      deviceId: 'd3',
      sessionId: 's3',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3_600_000),
    });
    const guard = personaGuard('Employer');
    runGuard(guard);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/candidate/jobs']);
  });

  it('redirects Employer to /employer when accessing candidate routes', () => {
    store.setSession({
      accessToken: 'tok',
      refreshTokenHash: 'h',
      userId: 'u4',
      deviceId: 'd4',
      sessionId: 's4',
      persona: 'Employer',
      expiresOn: new Date(Date.now() + 3_600_000),
    });
    const guard = personaGuard('Candidate');
    runGuard(guard);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/employer']);
  });

  it('redirects null persona (unauthenticated) to /candidate/jobs', () => {
    // No session set → persona is null
    const guard = personaGuard('Candidate');
    runGuard(guard);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/candidate/jobs']);
  });
});
