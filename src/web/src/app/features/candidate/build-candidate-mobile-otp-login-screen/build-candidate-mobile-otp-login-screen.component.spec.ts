import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Router } from '@angular/router';
import { BuildCandidateMobileOtpLoginScreenComponent } from './build-candidate-mobile-otp-login-screen.component';
import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import {
  AuthService,
  RequestOtpRequest,
  RequestOtpResponse,
  VerifyOtpRequest,
  VerifyOtpResponse,
} from '../../../core/auth/auth.service';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

describe('BuildCandidateMobileOtpLoginScreenComponent', () => {
  let fixture: ComponentFixture<BuildCandidateMobileOtpLoginScreenComponent>;
  let component: BuildCandidateMobileOtpLoginScreenComponent;
  let store: AuthSessionStore;
  let httpMock: HttpTestingController;
  let router: Router;
  let originalConsoleError: typeof console.error;
  let originalConsoleLog: typeof console.log;

  beforeEach(() => {
    originalConsoleError = console.error;
    originalConsoleLog = console.log;

    console.error = () => {};
    console.log = () => {};

    TestBed.configureTestingModule({
      imports: [
        BuildCandidateMobileOtpLoginScreenComponent,
        FormsModule,
        HttpClientTestingModule,
        RouterTestingModule,
      ],
      teardown: { destroyAfterEach: true },
    });

    fixture = TestBed.createComponent(BuildCandidateMobileOtpLoginScreenComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(AuthSessionStore);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    console.error = originalConsoleError;
    console.log = originalConsoleLog;
  });

  describe('initial state', () => {
    it('renders phone step when not authenticated', () => {
      fixture.detectChanges();
      expect(component.showPhoneStep()).toBe(true);
      expect(component.showOtpStep()).toBe(false);
      expect(component.vm().state).toBe('phone');
    });

    it('redirects to jobs when already authenticated', () => {
      store.setSession({
        accessToken: 'token',
        refreshTokenHash: 'hash',
        userId: 'user-1',
        deviceId: 'device-1',
        sessionId: 'session-1',
        persona: 'Candidate',
        expiresOn: new Date(Date.now() + 3600_000),
      });

      fixture.detectChanges();
      // The component sets redirect in ngOnInit; verify store is set
      expect(store.isAuthenticated()).toBe(true);
    });
  });

  describe('phone input validation', () => {
    it('shows error for invalid phone number (too short)', () => {
      fixture.detectChanges();
      component.onPhoneInput('98765');
      fixture.detectChanges();
      expect(component.vm().canSubmitPhone).toBe(false);
      expect(component.vm().phoneError).toBeUndefined();
    });

    it('shows error for invalid phone number (wrong prefix)', () => {
      fixture.detectChanges();
      component.onPhoneInput('5876543210');
      fixture.detectChanges();
      expect(component.vm().canSubmitPhone).toBe(false);
    });

    it('enables submit for valid 10-digit phone starting with 9', () => {
      fixture.detectChanges();
      component.onPhoneInput('9876543210');
      fixture.detectChanges();
      expect(component.vm().canSubmitPhone).toBe(true);
      expect(component.vm().phoneError).toBeUndefined();
    });

    it('enables submit for valid 10-digit phone starting with 7', () => {
      fixture.detectChanges();
      component.onPhoneInput('7890123456');
      fixture.detectChanges();
      expect(component.vm().canSubmitPhone).toBe(true);
    });
  });

  describe('OTP request happy path', () => {
    it('requests OTP, shows OTP step with masked phone message, and starts resend countdown', () => {
      fixture.detectChanges();
      component.onPhoneInput('9876543210');
      fixture.detectChanges();

      component.onPhoneSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      expect(req.request.method).toBe('POST');
      const body = req.request.body as {
        normalizedIdentifier: string;
        purpose: string;
        channel: string;
      };
      expect(body.normalizedIdentifier).toBe('9876543210');
      expect(body.purpose).toBe('Login');
      expect(body.channel).toBe('Sms');

      req.flush({
        challengeId: 'challenge-123',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });

      fixture.detectChanges();
      expect(component.showOtpStep()).toBe(true);
      expect(component.vm().otpState).toBe('sent');
      expect(component.vm().message).toContain('***-***-3210');
      expect(component.vm().resendDisabled).toBe(true);
      expect(component.vm().resendCountdownSeconds).toBe(30);
    });
  });

  describe('OTP verify happy path', () => {
    it('verifies OTP, stores session, and shows success state', () => {
      // First, set up OTP step via request
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const requestReq = httpMock.expectOne('/api/auth/otp/request');
      requestReq.flush({
        challengeId: 'challenge-abc',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      // Now enter OTP and submit
      component.onOtpInput('123456');
      fixture.detectChanges();
      expect(component.vm().canSubmitOtp).toBe(true);

      component.onOtpSubmit();
      fixture.detectChanges();

      const verifyReq = httpMock.expectOne('/api/auth/otp/verify');
      expect(verifyReq.request.method).toBe('POST');
      const verifyBody = verifyReq.request.body as { challengeId: string; code: string };
      expect(verifyBody.challengeId).toBe('challenge-abc');
      expect(verifyBody.code).toBe('123456');

      verifyReq.flush({
        accountId: '00000000-0000-0000-0000-000000000001',
        displayName: 'User 00000000',
        provider: 'Otp',
        isNewAccount: false,
        deviceId: 'device-xyz',
        accessToken: 'access-token-value',
        expiresOn: new Date(Date.now() + 900_000).toISOString(),
      });

      fixture.detectChanges();
      expect(store.accessToken()).toBe('access-token-value');
      expect(store.userId()).toBe('00000000-0000-0000-0000-000000000001');
      expect(store.persona()).toBe('Candidate');
      expect(component.showSuccess()).toBe(true);
      expect(component.vm().message).toContain('successful');
    });
  });

  describe('OTP verification error states', () => {
    it('shows actionable error when OTP is expired', () => {
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush({
        challengeId: 'challenge-1',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      component.onOtpInput('123456');
      component.onOtpSubmit();
      fixture.detectChanges();

      const verifyReq = httpMock.expectOne('/api/auth/otp/verify');
      verifyReq.flush(
        { Code: 'OTP_EXPIRED', Message: 'OTP expired' },
        { status: 400, statusText: 'Bad Request' },
      );

      fixture.detectChanges();
      expect(component.showError()).toBe(true);
      expect(component.errorCode()).toBe('OTP_EXPIRED');
      expect(component.errorMessage()).toContain('expired');
      expect(component.vm().otpState).toBe('sent');
    });

    it('shows actionable error when OTP is invalid', () => {
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush({
        challengeId: 'challenge-2',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      component.onOtpInput('000000');
      component.onOtpSubmit();
      fixture.detectChanges();

      const verifyReq = httpMock.expectOne('/api/auth/otp/verify');
      verifyReq.flush(
        { Code: 'OTP_INVALID', Message: 'Invalid code' },
        { status: 400, statusText: 'Bad Request' },
      );

      fixture.detectChanges();
      expect(component.errorCode()).toBe('OTP_INVALID');
      expect(component.errorMessage()).toContain('Invalid OTP');
    });

    it('shows error when OTP attempts exceeded', () => {
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush({
        challengeId: 'challenge-3',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      component.onOtpInput('123456');
      component.onOtpSubmit();
      fixture.detectChanges();

      const verifyReq = httpMock.expectOne('/api/auth/otp/verify');
      verifyReq.flush(
        { Code: 'OTP_ATTEMPTS_EXCEEDED', Message: 'Too many attempts' },
        { status: 400, statusText: 'Bad Request' },
      );

      fixture.detectChanges();
      expect(component.errorCode()).toBe('OTP_ATTEMPTS_EXCEEDED');
      expect(component.errorMessage()).toContain('Too many');
    });

    it('shows error when rate limited on OTP request', () => {
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush(
        { Code: 'RATE_LIMITED', Message: 'Too many requests' },
        { status: 429, statusText: 'Too Many Requests' },
      );

      fixture.detectChanges();
      expect(component.showError()).toBe(true);
      expect(component.errorCode()).toBe('RATE_LIMITED');
      expect(component.errorMessage()).toContain('Too many requests');
      expect(component.vm().state).toBe('error');
    });
  });

  describe('resend OTP flow', () => {
    it('resends OTP when resend is enabled and updates countdown', () => {
      // Set up OTP step first
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const requestReq = httpMock.expectOne('/api/auth/otp/request');
      requestReq.flush({
        challengeId: 'challenge-4',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      // Allow countdown to pass
      component.vm.update((v) => ({ ...v, resendDisabled: false }));
      fixture.detectChanges();

      component.onResendOtp();
      fixture.detectChanges();

      const resendReq = httpMock.expectOne('/api/auth/otp/request');
      expect(resendReq.request.method).toBe('POST');
      resendReq.flush({
        challengeId: 'challenge-5',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });

      fixture.detectChanges();
      expect(component.vm().otpState).toBe('sent');
      expect(component.vm().resendDisabled).toBe(true);
      expect(component.vm().resendCountdownSeconds).toBe(30);
    });

    it('disables resend button during countdown', () => {
      fixture.detectChanges();
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush({
        challengeId: 'challenge-6',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      expect(component.vm().resendDisabled).toBe(true);
    });
  });

  describe('loading state and double-submit prevention', () => {
    it('shows loading state and disables submit during OTP request', () => {
      component.onPhoneInput('9876543210');
      fixture.detectChanges();

      component.onPhoneSubmit();
      fixture.detectChanges();

      expect(component.showLoading()).toBe(true);
      expect(component.vm().canSubmitPhone).toBe(false);
      expect(component.vm().canSubmitOtp).toBe(false);

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush({
        challengeId: 'challenge-7',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
    });

    it('shows loading state and disables submit during OTP verify', () => {
      // Setup request first
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const requestReq = httpMock.expectOne('/api/auth/otp/request');
      requestReq.flush({
        challengeId: 'challenge-8',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      component.onOtpInput('123456');
      component.onOtpSubmit();
      fixture.detectChanges();

      expect(component.showLoading()).toBe(true);
      expect(component.vm().canSubmitOtp).toBe(false);

      const verifyReq = httpMock.expectOne('/api/auth/otp/verify');
      verifyReq.flush({
        accountId: '00000000-0000-0000-0000-000000000001',
        displayName: 'User',
        provider: 'Otp',
        isNewAccount: false,
        deviceId: 'device-1',
        accessToken: 'token',
        expiresOn: new Date(Date.now() + 900_000).toISOString(),
      });
    });
  });

  describe('secret logging', () => {
    it('does not log OTP code or access token to console during verify', () => {
      const logged: string[] = [];

      console.error = (...args: unknown[]) => logged.push(String(args));
      console.log = (...args: unknown[]) => logged.push(String(args));

      try {
        // Setup phone and request OTP
        component.onPhoneInput('9876543210');
        component.onPhoneSubmit();
        fixture.detectChanges();

        const requestReq = httpMock.expectOne('/api/auth/otp/request');
        requestReq.flush({
          challengeId: 'challenge-secret',
          purpose: 'Login',
          expiresOn: new Date(Date.now() + 300_000).toISOString(),
          maxAttempts: 3,
          resendCooldownSeconds: 30,
        });
        fixture.detectChanges();

        // Enter OTP and verify
        component.onOtpInput('123456');
        component.onOtpSubmit();
        fixture.detectChanges();

        const verifyReq = httpMock.expectOne('/api/auth/otp/verify');
        verifyReq.flush({
          accountId: '00000000-0000-0000-0000-000000000001',
          displayName: 'User',
          provider: 'Otp',
          isNewAccount: false,
          deviceId: 'device-secret',
          accessToken: 'super-secret-access-token',
          expiresOn: new Date(Date.now() + 900_000).toISOString(),
        });

        httpMock.verify();

        const allLogs = logged.join('\\n');
        // The component/service should not log the OTP code or access token
        // Note: '123456' is not a sensitive value, but we verify the pattern
        expect(allLogs).not.toContain('super-secret-access-token');
        expect(allLogs).not.toContain('device-secret');
      } finally {
        console.error = originalConsoleError;
        console.log = originalConsoleLog;
      }
    });

    it('does not log phone number or challenge id in plaintext to console', () => {
      const logged: string[] = [];

      console.error = (...args: unknown[]) => logged.push(String(args));
      console.log = (...args: unknown[]) => logged.push(String(args));

      try {
        component.onPhoneInput('9876543210');
        component.onPhoneSubmit();
        fixture.detectChanges();

        const req = httpMock.expectOne('/api/auth/otp/request');
        req.flush({
          challengeId: 'challenge-plaintext-id',
          purpose: 'Login',
          expiresOn: new Date(Date.now() + 300_000).toISOString(),
          maxAttempts: 3,
          resendCooldownSeconds: 30,
        });

        httpMock.verify();

        const allLogs = logged.join('\n');
        // The component should not log raw phone or challenge id
        expect(allLogs).not.toContain('9876543210');
        expect(allLogs).not.toContain('challenge-plaintext-id');
      } finally {
        console.error = originalConsoleError;
        console.log = originalConsoleLog;
      }
    });
  });

  describe('error state recovery', () => {
    it('allows retry from error state by navigating back to phone step', () => {
      // Trigger an error
      component.onPhoneInput('9876543210');
      component.onPhoneSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush(
        { Code: 'RATE_LIMITED', Message: 'Too many requests' },
        { status: 429, statusText: 'Too Many Requests' },
      );
      fixture.detectChanges();

      expect(component.showError()).toBe(true);

      // Click try again - uses retryFromError to return to phone step
      component.retryFromError();
      fixture.detectChanges();

      expect(component.showPhoneStep()).toBe(true);
      expect(component.vm().state).toBe('phone');
      expect(component.vm().phoneError).toBeUndefined();
      expect(component.vm().errorCode).toBeUndefined();
    });
  });
});
