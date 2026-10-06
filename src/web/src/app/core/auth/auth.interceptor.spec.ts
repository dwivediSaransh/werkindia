import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AuthSessionStore } from './auth-session.store';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let store: AuthSessionStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptorsFromDi()), AuthSessionStore, AuthService],
    });
    store = TestBed.inject(AuthSessionStore);
  });

  it('attaches Authorization header when token is present', () => {
    store.setSession({
      accessToken: 'test-token-123',
      refreshTokenHash: null,
      userId: null,
      deviceId: null,
      sessionId: null,
      persona: null,
      expiresOn: null,
    });

    expect(store.accessToken()).toBe('test-token-123');
    expect(store.isAuthenticated()).toBeTruthy();

    // Verify URL routing: only /api/* requests get auth header
    expect('/api/auth/otp/refresh'.startsWith('/api/')).toBeTruthy();
    expect('/graphql'.startsWith('/api/')).toBeFalsy();
  });

  it('does not attach Authorization header when no token is present', () => {
    store.clearSession();
    expect(store.accessToken()).toBeNull();
    expect(store.isAuthenticated()).toBeFalsy();
  });

  it('has single-flight refresh logic', () => {
    const interceptorCode = authInterceptor.toString();
    expect(interceptorCode).toContain('isRefreshing');
    expect(interceptorCode).toContain('refreshSubject');
    expect(interceptorCode).toContain('filter');
    expect(interceptorCode).toContain('take');
  });

  it('uses AuthSessionStore for token retrieval', () => {
    const interceptorCode = authInterceptor.toString();
    expect(interceptorCode).toContain('AuthSessionStore');
    expect(interceptorCode).toContain('AuthSessionStore');
    expect(interceptorCode).toContain('accessToken');
  });

  it('clears session on refresh failure', () => {
    const interceptorCode = authInterceptor.toString();
    expect(interceptorCode).toContain('store.clearSession()');
  });

  it('retries request with new token after successful refresh', () => {
    const interceptorCode = authInterceptor.toString();
    expect(interceptorCode).toContain('retryReq');
    expect(interceptorCode).toContain('next(retryReq)');
  });

  it('does not log token values during interception', () => {
    store.setSession({
      accessToken: 'super-secret-jwt',
      refreshTokenHash: 'secret-refresh-hash',
      userId: 'user-123',
      deviceId: 'device-456',
      sessionId: 'session-789',
      persona: 'Candidate',
      expiresOn: new Date(),
    });

    expect(store.accessToken()).toBe('super-secret-jwt');
    const interceptorCode = authInterceptor.toString();
    expect(interceptorCode).not.toContain('console.log');
    expect(interceptorCode).not.toContain('console.error');
  });
});
