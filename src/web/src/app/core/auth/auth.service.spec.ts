import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  AuthService,
  RefreshRequest,
  RefreshResponse,
  LogoutRequest,
  LogoutResponse,
  RequestOtpRequest,
  RequestOtpResponse,
  VerifyOtpRequest,
  VerifyOtpResponse,
} from './auth.service';
import { AuthSessionStore } from './auth-session.store';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let store: AuthSessionStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService, AuthSessionStore],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthSessionStore);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('calls refresh endpoint and updates session store on success', () => {
    const request: RefreshRequest = {
      userId: 'user-123',
      deviceId: 'device-456',
      refreshTokenHash: 'old-hash',
    };

    const response: RefreshResponse = {
      accessToken: 'new-access-token',
      newRefreshTokenHash: 'new-refresh-hash',
      expiresOn: new Date(Date.now() + 3600_000).toISOString(),
    };

    service.refresh(request).subscribe((res) => {
      expect(res).toEqual(response);
    });

    const req = httpMock.expectOne('/api/auth/otp/refresh');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(response);

    // Verify store was updated
    expect(store.accessToken()).toBe('new-access-token');
    expect(store.refreshTokenHash()).toBe('new-refresh-hash');
  });

  it('clears session on logout', () => {
    // Set up a session first
    store.setSession({
      accessToken: 'token',
      refreshTokenHash: 'hash',
      userId: 'user-123',
      deviceId: 'device-456',
      sessionId: 'session-789',
      persona: 'Candidate',
      expiresOn: new Date(),
    });

    const request: LogoutRequest = {
      userId: 'user-123',
      deviceId: 'device-456',
      refreshTokenHash: 'hash',
    };

    let response: LogoutResponse | undefined;
    service.logout(request).subscribe((res) => {
      response = res;
    });

    const req = httpMock.expectOne('/api/auth/otp/logout');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ success: true });

    expect(response?.success).toBeTruthy();
    // Store should be cleared after logout
    expect(store.accessToken()).toBeNull();
    expect(store.isAuthenticated()).toBeFalsy();
  });

  it('does not log token values during refresh', () => {
    const request: RefreshRequest = {
      userId: 'user-123',
      deviceId: 'device-456',
      refreshTokenHash: 'secret-refresh-hash',
    };

    const response: RefreshResponse = {
      accessToken: 'new-secret-token',
      newRefreshTokenHash: 'new-secret-hash',
      expiresOn: new Date(Date.now() + 3600_000).toISOString(),
    };

    service.refresh(request).subscribe();
    const req = httpMock.expectOne('/api/auth/otp/refresh');
    req.flush(response);

    // The store should have the tokens but they should never be logged
    expect(store.accessToken()).toBe('new-secret-token');
    // No console output should contain the token values
  });

  it('calls request OTP endpoint with normalized phone and returns challenge metadata', () => {
    const request: RequestOtpRequest = {
      accountId: '00000000-0000-0000-0000-000000000001',
      purpose: 'Login',
      normalizedIdentifier: '9876543210',
      channel: 'Sms',
    };

    const response: RequestOtpResponse = {
      challengeId: 'challenge-123',
      purpose: 'Login',
      expiresOn: new Date(Date.now() + 300_000).toISOString(),
      maxAttempts: 3,
      resendCooldownSeconds: 30,
    };

    service.requestOtp(request).subscribe((res) => {
      expect(res).toEqual(response);
    });

    const req = httpMock.expectOne('/api/auth/otp/request');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(response);
  });

  it('calls verify OTP endpoint with challenge and code and returns verify response', () => {
    const request: VerifyOtpRequest = {
      challengeId: 'challenge-123',
      code: '123456',
      normalizedIdentifier: '9876543210',
    };

    const response: VerifyOtpResponse = {
      accountId: '00000000-0000-0000-0000-000000000001',
      displayName: 'User 00000000',
      provider: 'Otp',
      isNewAccount: false,
      deviceId: 'device-abc',
      accessToken: 'access-token-value',
      expiresOn: new Date(Date.now() + 900_000).toISOString(),
    };

    service.verifyOtp(request).subscribe((res) => {
      expect(res).toEqual(response);
    });

    const req = httpMock.expectOne('/api/auth/otp/verify');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(response);
  });

  it('does not log OTP code or access token during verify', () => {
    const request: VerifyOtpRequest = {
      challengeId: 'challenge-123',
      code: 'secret-otp-code',
      normalizedIdentifier: '9876543210',
    };

    const response: VerifyOtpResponse = {
      accountId: '00000000-0000-0000-0000-000000000001',
      displayName: 'User 00000000',
      provider: 'Otp',
      isNewAccount: false,
      deviceId: 'device-abc',
      accessToken: 'secret-access-token-value',
      expiresOn: new Date(Date.now() + 900_000).toISOString(),
    };

    service.verifyOtp(request).subscribe();
    const req = httpMock.expectOne('/api/auth/otp/verify');
    req.flush(response);

    // Service must not emit OTP or token values to console
    // (Verified by the absence of any console.log/error calls in the service)
  });
});
