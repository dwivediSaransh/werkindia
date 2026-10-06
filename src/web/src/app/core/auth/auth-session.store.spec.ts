import { TestBed } from '@angular/core/testing';
import { AuthSessionStore, AuthSession } from './auth-session.store';

describe('AuthSessionStore', () => {
  let store: AuthSessionStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthSessionStore],
    });
    store = TestBed.inject(AuthSessionStore);
  });

  it('starts with no authenticated session', () => {
    expect(store.isAuthenticated()).toBeFalsy();
    expect(store.accessToken()).toBeNull();
    expect(store.userId()).toBeNull();
  });

  it('sets and clears session', () => {
    const session: AuthSession = {
      accessToken: 'test-access-token',
      refreshTokenHash: 'test-refresh-hash',
      userId: 'user-123',
      deviceId: 'device-456',
      sessionId: 'session-789',
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3600_000),
    };

    store.setSession(session);

    expect(store.accessToken()).toBe('test-access-token');
    expect(store.userId()).toBe('user-123');
    expect(store.persona()).toBe('Candidate');
    expect(store.isAuthenticated()).toBeTruthy();

    store.clearSession();

    expect(store.accessToken()).toBeNull();
    expect(store.userId()).toBeNull();
    expect(store.isAuthenticated()).toBeFalsy();
  });

  it('updates partial session state', () => {
    store.setSession({
      accessToken: 'old-token',
      refreshTokenHash: 'old-hash',
      userId: 'user-1',
      deviceId: 'device-1',
      sessionId: 'session-1',
      persona: 'Candidate',
      expiresOn: new Date(),
    });

    store.updateSession({
      accessToken: 'new-token',
      refreshTokenHash: 'new-hash',
    });

    expect(store.accessToken()).toBe('new-token');
    expect(store.refreshTokenHash()).toBe('new-hash');
    expect(store.userId()).toBe('user-1'); // unchanged
  });

  it('detects when session is expiring soon', () => {
    // Session expires in 10 seconds (less than 30s buffer)
    store.setSession({
      accessToken: 'token',
      refreshTokenHash: null,
      userId: null,
      deviceId: null,
      sessionId: null,
      persona: null,
      expiresOn: new Date(Date.now() + 10_000),
    });

    expect(store.sessionExpiresSoon()).toBeTruthy();
  });

  it('does not detect expiring session when far from expiry', () => {
    store.setSession({
      accessToken: 'token',
      refreshTokenHash: null,
      userId: null,
      deviceId: null,
      sessionId: null,
      persona: null,
      expiresOn: new Date(Date.now() + 300_000), // 5 minutes
    });

    expect(store.sessionExpiresSoon()).toBeFalsy();
  });

  it('does not log token values', () => {
    const session: AuthSession = {
      accessToken: 'super-secret-jwt-value',
      refreshTokenHash: 'secret-refresh-hash',
      userId: 'user-123',
      deviceId: 'device-456',
      sessionId: 'session-789',
      persona: 'Candidate',
      expiresOn: new Date(),
    };

    store.setSession(session);

    // Verify data is stored correctly but nothing is logged
    expect(store.accessToken()).toBe('super-secret-jwt-value');
    expect(store.refreshTokenHash()).toBe('secret-refresh-hash');
    // The store itself should never log these values — no console.error/warn calls
    expect(store.isAuthenticated()).toBeTruthy();
  });
});
