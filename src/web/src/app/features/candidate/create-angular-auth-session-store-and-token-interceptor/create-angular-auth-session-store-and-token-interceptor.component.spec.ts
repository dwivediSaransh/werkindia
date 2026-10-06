import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { CreateAngularAuthSessionStoreAndTokenInterceptorComponent } from './create-angular-auth-session-store-and-token-interceptor.component';
import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import { AuthService } from '../../../core/auth/auth.service';

describe('CreateAngularAuthSessionStoreAndTokenInterceptorComponent', () => {
  let fixture: ComponentFixture<CreateAngularAuthSessionStoreAndTokenInterceptorComponent>;
  let component: CreateAngularAuthSessionStoreAndTokenInterceptorComponent;
  let store: AuthSessionStore;
  let httpMock: HttpTestingController;
  let originalConsoleError: typeof console.error;
  let originalConsoleLog: typeof console.log;

  beforeEach(() => {
    originalConsoleError = console.error;
    originalConsoleLog = console.log;

    console.error = () => {};
    console.log = () => {};

    TestBed.configureTestingModule({
      imports: [CreateAngularAuthSessionStoreAndTokenInterceptorComponent, HttpClientTestingModule],
      teardown: { destroyAfterEach: true },
    });

    fixture = TestBed.createComponent(CreateAngularAuthSessionStoreAndTokenInterceptorComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(AuthSessionStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    console.error = originalConsoleError;
    console.log = originalConsoleLog;
  });

  it('renders idle state before a session is set and does not allow refresh or logout', () => {
    fixture.detectChanges();
    expect(component.isAuthenticated).toBe(false);
    expect(component.vm().state).toBe('idle');
    expect(component.isRefreshDisabled).toBe(true);
  });

  it('sets a mock session and renders authenticated state with persona and userId', () => {
    component.setMockSession();
    fixture.detectChanges();

    expect(component.isAuthenticated).toBe(true);
    expect(component.persona).toBe('Candidate');
    expect(component.userId).not.toBeNull();
    expect(component.vm().state).toBe('authenticated');
    expect(component.vm().message).toBe('Mock session set successfully');
  });

  it('prevents double-submitting the refresh action and shows a loading message', () => {
    store.setSession({
      accessToken: 'token',
      refreshTokenHash: 'hash',
      userId: 'user-1',
      deviceId: 'device-1',
      sessionId: 'session-1',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3600_000),
    });

    component.triggerRefresh();
    fixture.detectChanges();
    expect(component.refreshInProgress).toBe(true);
    expect(component.vm().state).toBe('loading');
    expect(component.vm().message).toBe('Refreshing token...');

    // Second call while first is still in progress should hit the guard
    component.triggerRefresh();
    fixture.detectChanges();
    expect(component.vm().state).toBe('loading');
    expect(component.vm().message).toBe('Refresh already in progress...');

    // Flush the pending request from the first call so afterEach verify() passes
    const req = httpMock.expectOne('/api/auth/otp/refresh');
    req.flush({
      accessToken: 'new-token',
      newRefreshTokenHash: 'new-hash',
      expiresOn: new Date(Date.now() + 3600_000).toISOString(),
    });
  });

  it('shows an actionable error state when refresh fails with a stable error code', () => {
    store.setSession({
      accessToken: 'token',
      refreshTokenHash: 'hash',
      userId: 'user-1',
      deviceId: 'device-1',
      sessionId: 'session-1',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3600_000),
    });

    component.triggerRefresh();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/auth/otp/refresh');
    expect(req.request.method).toBe('POST');

    req.flush(
      { Code: 'UNAUTHENTICATED', Message: 'Session expired' },
      { status: 401, statusText: 'Unauthorized' },
    );

    fixture.detectChanges();

    const vm = component.vm();
    expect(vm.state).toBe('error');
    expect(vm.message).toContain('UNAUTHENTICATED');
    expect(component.isAuthenticated).toBe(false);
  });

  it('renders success after a valid refresh and updates the session store', () => {
    store.setSession({
      accessToken: 'old-token',
      refreshTokenHash: 'old-hash',
      userId: 'user-1',
      deviceId: 'device-1',
      sessionId: 'session-1',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3600_000),
    });

    component.triggerRefresh();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/auth/otp/refresh');
    req.flush({
      accessToken: 'new-token',
      newRefreshTokenHash: 'new-hash',
      expiresOn: new Date(Date.now() + 7200_000).toISOString(),
    });

    fixture.detectChanges();

    expect(store.accessToken()).toBe('new-token');
    expect(store.refreshTokenHash()).toBe('new-hash');
    expect(component.vm().state).toBe('authenticated');
    expect(component.vm().message).toBe('Token refreshed successfully');
  });

  it('shows an error when refresh is attempted without required session fields', () => {
    store.setSession({
      accessToken: 'token',
      refreshTokenHash: null,
      userId: null,
      deviceId: null,
      sessionId: null,
      persona: null,
      expiresOn: null,
    });

    component.triggerRefresh();
    fixture.detectChanges();

    expect(component.vm().state).toBe('error');
    expect(component.vm().message).toContain('No valid session to refresh');
    expect(component.refreshInProgress).toBe(false);
  });

  it('clears the session store and renders idle after logout', () => {
    store.setSession({
      accessToken: 'token',
      refreshTokenHash: 'hash',
      userId: 'user-1',
      deviceId: 'device-1',
      sessionId: 'session-1',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3600_000),
    });

    component.logout();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/auth/otp/logout');
    expect(req.request.method).toBe('POST');
    req.flush({ success: true });

    fixture.detectChanges();

    expect(store.accessToken()).toBeNull();
    expect(component.isAuthenticated).toBe(false);
    expect(component.vm().state).toBe('idle');
    expect(component.vm().message).toBe('Logged out successfully');
  });

  it('clears the session locally even when logout response fails', () => {
    store.setSession({
      accessToken: 'token',
      refreshTokenHash: 'hash',
      userId: 'user-1',
      deviceId: 'device-1',
      sessionId: 'session-1',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3600_000),
    });

    component.logout();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/auth/otp/logout');
    req.flush(null, { status: 500, statusText: 'Server Error' });

    fixture.detectChanges();

    expect(store.accessToken()).toBeNull();
    expect(component.isAuthenticated).toBe(false);
    expect(component.vm().state).toBe('idle');
    expect(component.vm().message).toBe('Logged out (session cleared)');
  });

  it('does not log token or contact values to the console during actions', () => {
    const logged: string[] = [];

    console.error = (...args: unknown[]) => logged.push(String(args));
    console.log = (...args: unknown[]) => logged.push(String(args));

    try {
      component.setMockSession();
      fixture.detectChanges();

      store.setSession({
        accessToken: 'super-secret-jwt-value',
        refreshTokenHash: 'secret-refresh-hash',
        userId: 'user-123',
        deviceId: 'device-456',
        sessionId: 'session-789',
        persona: 'Candidate',
        expiresOn: new Date(Date.now() + 3600_000),
      });

      component.triggerRefresh();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/refresh');
      req.flush({
        accessToken: 'new-secret-token',
        newRefreshTokenHash: 'new-secret-hash',
        expiresOn: new Date(Date.now() + 3600_000).toISOString(),
      });

      httpMock.verify();

      expect(logged.join('\n')).not.toContain('super-secret-jwt-value');
      expect(logged.join('\n')).not.toContain('new-secret-token');
      expect(logged.join('\n')).not.toContain('secret-refresh-hash');
      expect(logged.join('\n')).not.toContain('new-secret-hash');
    } finally {
      console.error = originalConsoleError;
      console.log = originalConsoleLog;
    }
  });
});
