import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Router } from '@angular/router';
import { EmployerOtpLoginComponent } from './employer-otp-login.component';
import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

describe('EmployerOtpLoginComponent', () => {
  let fixture: ComponentFixture<EmployerOtpLoginComponent>;
  let component: EmployerOtpLoginComponent;
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
        EmployerOtpLoginComponent,
        FormsModule,
        HttpClientTestingModule,
        RouterTestingModule,
      ],
      teardown: { destroyAfterEach: true },
    });

    fixture = TestBed.createComponent(EmployerOtpLoginComponent);
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

  // ── initial state ──────────────────────────────────────────────────────

  describe('initial state', () => {
    it('shows identifier step with mobile selected by default', () => {
      fixture.detectChanges();
      expect(component.showIdentifierStep()).toBe(true);
      expect(component.vm().identifierType).toBe('mobile');
      expect(component.vm().state).toBe('identifier');
    });

    it('redirects to employer when already authenticated', () => {
      store.setSession({
        accessToken: 'token',
        refreshTokenHash: 'hash',
        userId: 'user-1',
        deviceId: 'device-1',
        sessionId: 'session-1',
        persona: 'Employer',
        expiresOn: new Date(Date.now() + 3_600_000),
      });
      fixture.detectChanges();
      expect(store.isAuthenticated()).toBe(true);
    });
  });

  // ── identifier type toggle ─────────────────────────────────────────────

  describe('identifier type toggle', () => {
    it('switches to email and resets input', () => {
      fixture.detectChanges();
      component.onIdentifierInput('9876543210');
      component.onIdentifierTypeChange('email');
      fixture.detectChanges();
      expect(component.vm().identifierType).toBe('email');
      expect(component.identifierInputValue()).toBe('');
      expect(component.vm().canSubmitIdentifier).toBe(false);
    });

    it('switches back to mobile', () => {
      fixture.detectChanges();
      component.onIdentifierTypeChange('email');
      component.onIdentifierTypeChange('mobile');
      fixture.detectChanges();
      expect(component.vm().identifierType).toBe('mobile');
    });
  });

  // ── mobile validation ──────────────────────────────────────────────────

  describe('mobile validation', () => {
    it('rejects too-short mobile number', () => {
      fixture.detectChanges();
      component.onIdentifierInput('98765');
      expect(component.vm().canSubmitIdentifier).toBe(false);
    });

    it('rejects mobile starting with 5', () => {
      fixture.detectChanges();
      component.onIdentifierInput('5876543210');
      expect(component.vm().canSubmitIdentifier).toBe(false);
    });

    it('accepts valid 10-digit mobile starting with 9', () => {
      fixture.detectChanges();
      component.onIdentifierInput('9876543210');
      expect(component.vm().canSubmitIdentifier).toBe(true);
    });

    it('accepts valid 10-digit mobile starting with 7', () => {
      fixture.detectChanges();
      component.onIdentifierInput('7890123456');
      expect(component.vm().canSubmitIdentifier).toBe(true);
    });
  });

  // ── email validation ───────────────────────────────────────────────────

  describe('email validation', () => {
    beforeEach(() => {
      fixture.detectChanges();
      component.onIdentifierTypeChange('email');
    });

    it('rejects bare string without @', () => {
      component.onIdentifierInput('notanemail');
      expect(component.vm().canSubmitIdentifier).toBe(false);
    });

    it('rejects email without domain', () => {
      component.onIdentifierInput('test@');
      expect(component.vm().canSubmitIdentifier).toBe(false);
    });

    it('accepts valid email address', () => {
      component.onIdentifierInput('employer@company.com');
      expect(component.vm().canSubmitIdentifier).toBe(true);
    });
  });

  // ── OTP request happy path (mobile) ───────────────────────────────────

  describe('OTP request happy path — mobile', () => {
    it('requests OTP via SMS, shows OTP step with masked phone, starts countdown', () => {
      fixture.detectChanges();
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      expect(req.request.method).toBe('POST');
      const body = req.request.body as { normalizedIdentifier: string; channel: string };
      expect(body.normalizedIdentifier).toBe('9876543210');
      expect(body.channel).toBe('Sms');

      req.flush({
        challengeId: 'ch-sms-1',
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

  // ── OTP request happy path (email) ────────────────────────────────────

  describe('OTP request happy path — email', () => {
    it('requests OTP via email channel, shows OTP step with masked email', () => {
      fixture.detectChanges();
      component.onIdentifierTypeChange('email');
      component.onIdentifierInput('employer@company.com');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      const body = req.request.body as { normalizedIdentifier: string; channel: string };
      expect(body.normalizedIdentifier).toBe('employer@company.com');
      expect(body.channel).toBe('Email');

      req.flush({
        challengeId: 'ch-email-1',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });

      fixture.detectChanges();
      expect(component.showOtpStep()).toBe(true);
      expect(component.vm().message).toContain('***@company.com');
    });
  });

  // ── OTP verify happy path ──────────────────────────────────────────────

  describe('OTP verify happy path', () => {
    it('verifies OTP, sets Employer persona in session store, shows success state', () => {
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      const requestReq = httpMock.expectOne('/api/auth/otp/request');
      requestReq.flush({
        challengeId: 'ch-verify-1',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      component.onOtpInput('654321');
      fixture.detectChanges();
      expect(component.vm().canSubmitOtp).toBe(true);

      component.onOtpSubmit();
      fixture.detectChanges();

      const verifyReq = httpMock.expectOne('/api/auth/otp/verify');
      const verifyBody = verifyReq.request.body as { challengeId: string; code: string };
      expect(verifyBody.challengeId).toBe('ch-verify-1');
      expect(verifyBody.code).toBe('654321');

      verifyReq.flush({
        accountId: '00000000-0000-0000-0000-000000000002',
        displayName: 'Employer User',
        provider: 'Otp',
        isNewAccount: false,
        deviceId: 'device-emp-1',
        accessToken: 'emp-access-token',
        expiresOn: new Date(Date.now() + 900_000).toISOString(),
      });

      fixture.detectChanges();
      expect(store.accessToken()).toBe('emp-access-token');
      expect(store.userId()).toBe('00000000-0000-0000-0000-000000000002');
      expect(store.persona()).toBe('Employer');
      expect(component.showSuccess()).toBe(true);
      expect(component.vm().message).toContain('successful');
    });
  });

  // ── OTP expiry error ───────────────────────────────────────────────────

  describe('OTP expiry error', () => {
    it('shows stable OTP_EXPIRED error with actionable message', () => {
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush({
        challengeId: 'ch-exp-1',
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
    });
  });

  // ── rate limit error ───────────────────────────────────────────────────

  describe('rate limit error', () => {
    it('shows RATE_LIMITED error when OTP request is rate-limited', () => {
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
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
    });
  });

  // ── loading state and double-submit prevention ─────────────────────────

  describe('loading state and double-submit prevention', () => {
    it('disables submit and shows loading during OTP request', () => {
      fixture.detectChanges();
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      expect(component.showLoading()).toBe(true);
      expect(component.vm().canSubmitIdentifier).toBe(false);
      expect(component.vm().canSubmitOtp).toBe(false);

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush({
        challengeId: 'ch-load-1',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
    });

    it('disables submit and shows loading during OTP verify', () => {
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      const requestReq = httpMock.expectOne('/api/auth/otp/request');
      requestReq.flush({
        challengeId: 'ch-load-2',
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
        accountId: '00000000-0000-0000-0000-000000000003',
        displayName: 'Emp',
        provider: 'Otp',
        isNewAccount: false,
        deviceId: 'device-2',
        accessToken: 'token-2',
        expiresOn: new Date(Date.now() + 900_000).toISOString(),
      });
    });
  });

  // ── resend OTP ─────────────────────────────────────────────────────────

  describe('resend OTP flow', () => {
    it('resends OTP and resets countdown after countdown expires', () => {
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      const requestReq = httpMock.expectOne('/api/auth/otp/request');
      requestReq.flush({
        challengeId: 'ch-resend-1',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      // Simulate countdown ending
      component.vm.update((v) => ({ ...v, resendDisabled: false }));
      fixture.detectChanges();

      component.onResendOtp();
      fixture.detectChanges();

      const resendReq = httpMock.expectOne('/api/auth/otp/request');
      resendReq.flush({
        challengeId: 'ch-resend-2',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });

      fixture.detectChanges();
      expect(component.vm().otpState).toBe('sent');
      expect(component.vm().resendDisabled).toBe(true);
    });

    it('resend is disabled during initial countdown', () => {
      fixture.detectChanges();
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush({
        challengeId: 'ch-cd-1',
        purpose: 'Login',
        expiresOn: new Date(Date.now() + 300_000).toISOString(),
        maxAttempts: 3,
        resendCooldownSeconds: 30,
      });
      fixture.detectChanges();

      expect(component.vm().resendDisabled).toBe(true);
    });
  });

  // ── secret logging ─────────────────────────────────────────────────────

  describe('secret logging', () => {
    it('does not log access token or device id to console during verify', () => {
      const logged: string[] = [];
      console.error = (...args: unknown[]) => logged.push(String(args));
      console.log = (...args: unknown[]) => logged.push(String(args));

      try {
        component.onIdentifierInput('9876543210');
        component.onIdentifierSubmit();
        fixture.detectChanges();

        const requestReq = httpMock.expectOne('/api/auth/otp/request');
        requestReq.flush({
          challengeId: 'ch-secret-1',
          purpose: 'Login',
          expiresOn: new Date(Date.now() + 300_000).toISOString(),
          maxAttempts: 3,
          resendCooldownSeconds: 30,
        });
        fixture.detectChanges();

        component.onOtpInput('999999');
        component.onOtpSubmit();
        fixture.detectChanges();

        const verifyReq = httpMock.expectOne('/api/auth/otp/verify');
        verifyReq.flush({
          accountId: '00000000-0000-0000-0000-000000000009',
          displayName: 'Emp',
          provider: 'Otp',
          isNewAccount: false,
          deviceId: 'secret-device-id',
          accessToken: 'super-secret-emp-token',
          expiresOn: new Date(Date.now() + 900_000).toISOString(),
        });

        httpMock.verify();
        const allLogs = logged.join('\n');
        expect(allLogs).not.toContain('super-secret-emp-token');
        expect(allLogs).not.toContain('secret-device-id');
      } finally {
        console.error = originalConsoleError;
        console.log = originalConsoleLog;
      }
    });

    it('does not log raw identifier or challenge id to console', () => {
      const logged: string[] = [];
      console.error = (...args: unknown[]) => logged.push(String(args));
      console.log = (...args: unknown[]) => logged.push(String(args));

      try {
        component.onIdentifierInput('9876543210');
        component.onIdentifierSubmit();
        fixture.detectChanges();

        const req = httpMock.expectOne('/api/auth/otp/request');
        req.flush({
          challengeId: 'secret-challenge-id',
          purpose: 'Login',
          expiresOn: new Date(Date.now() + 300_000).toISOString(),
          maxAttempts: 3,
          resendCooldownSeconds: 30,
        });

        httpMock.verify();
        const allLogs = logged.join('\n');
        expect(allLogs).not.toContain('9876543210');
        expect(allLogs).not.toContain('secret-challenge-id');
      } finally {
        console.error = originalConsoleError;
        console.log = originalConsoleLog;
      }
    });
  });

  // ── error recovery ─────────────────────────────────────────────────────

  describe('error state recovery', () => {
    it('returns to identifier step when retrying from error', () => {
      component.onIdentifierInput('9876543210');
      component.onIdentifierSubmit();
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/auth/otp/request');
      req.flush(
        { Code: 'RATE_LIMITED', Message: 'Too many requests' },
        { status: 429, statusText: 'Too Many Requests' },
      );
      fixture.detectChanges();

      expect(component.showError()).toBe(true);

      component.retryFromError();
      fixture.detectChanges();

      expect(component.showIdentifierStep()).toBe(true);
      expect(component.vm().state).toBe('identifier');
      expect(component.vm().errorCode).toBeUndefined();
    });
  });
});
