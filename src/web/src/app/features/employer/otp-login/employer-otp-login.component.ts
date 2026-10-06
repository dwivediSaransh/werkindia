import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService, RequestOtpRequest, VerifyOtpRequest } from '../../../core/auth/auth.service';
import { AuthSessionStore } from '../../../core/auth/auth-session.store';

type IdentifierType = 'mobile' | 'email';
type ViewState = 'identifier' | 'otp' | 'loading' | 'error' | 'success';
type OtpState = 'idle' | 'sent' | 'verified';

interface ViewModel {
  state: ViewState;
  identifierType: IdentifierType;
  message?: string;
  errorCode?: string;
  identifierError?: string;
  otpError?: string;
  otpState: OtpState;
  resendDisabled: boolean;
  resendCountdownSeconds: number;
  canSubmitIdentifier: boolean;
  canSubmitOtp: boolean;
}

const RESEND_COOLDOWN_SECONDS = 30;
const MOBILE_REGEX = /^[6-9]\d{9}$/;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  selector: 'app-employer-otp-login',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './employer-otp-login.component.html',
  styleUrl: './employer-otp-login.component.scss',
})
export class EmployerOtpLoginComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly store = inject(AuthSessionStore);

  readonly vm = signal<ViewModel>({
    state: 'identifier',
    identifierType: 'mobile',
    otpState: 'idle',
    resendDisabled: true,
    resendCountdownSeconds: 0,
    canSubmitIdentifier: false,
    canSubmitOtp: false,
  });

  private identifierValue = '';
  private otpCode = '';
  private challengeId = '';
  private normalizedIdentifier = '';
  private accountId = '';
  private countdownTimer: ReturnType<typeof setInterval> | null = null;

  // Template-bound reads (Angular OnPush: use functions not getters)
  readonly identifierInputValue = () => this.identifierValue;
  readonly otpInputValue = () => this.otpCode;

  readonly showIdentifierStep = computed(() => this.vm().state === 'identifier');
  readonly showOtpStep = computed(() => this.vm().state === 'otp');
  readonly showLoading = computed(() => this.vm().state === 'loading');
  readonly showError = computed(() => this.vm().state === 'error');
  readonly showSuccess = computed(() => this.vm().state === 'success');
  readonly errorMessage = computed(() => this.vm().message);
  readonly errorCode = computed(() => this.vm().errorCode);

  ngOnInit(): void {
    if (this.store.isAuthenticated()) {
      this.redirectToEmployer();
    }
  }

  ngOnDestroy(): void {
    this.clearCountdown();
  }

  onIdentifierTypeChange(type: IdentifierType): void {
    this.identifierValue = '';
    this.vm.update((v) => ({
      ...v,
      identifierType: type,
      identifierError: undefined,
      canSubmitIdentifier: false,
    }));
  }

  onIdentifierInput(value: string): void {
    const type = this.vm().identifierType;
    if (type === 'mobile') {
      this.identifierValue = value.replace(/\D/g, '').slice(0, 10);
    } else {
      this.identifierValue = value.trim().slice(0, 254);
    }
    this.vm.update((v) => ({
      ...v,
      identifierError: undefined,
      canSubmitIdentifier: this.isValidIdentifier(),
    }));
  }

  onIdentifierSubmit(): void {
    if (!this.isValidIdentifier()) {
      this.vm.update((v) => ({
        ...v,
        identifierError: this.buildValidationError(),
      }));
      return;
    }

    this.normalizedIdentifier = this.identifierValue.trim();
    this.clearError();
    this.setLoading();

    const type = this.vm().identifierType;
    const request: RequestOtpRequest = {
      accountId: this.generateAccountId(),
      purpose: 'Login',
      normalizedIdentifier: this.normalizedIdentifier,
      channel: type === 'mobile' ? 'Sms' : 'Email',
    };

    this.authService.requestOtp(request).subscribe({
      next: (response) => {
        this.challengeId = response.challengeId;
        this.accountId = request.accountId;
        const maskedTarget =
          type === 'mobile'
            ? this.maskMobile(this.normalizedIdentifier)
            : this.maskEmail(this.normalizedIdentifier);
        this.vm.set({
          state: 'otp',
          identifierType: type,
          otpState: 'sent',
          message: `OTP sent to ${maskedTarget}`,
          resendDisabled: true,
          resendCountdownSeconds: RESEND_COOLDOWN_SECONDS,
          canSubmitIdentifier: true,
          canSubmitOtp: false,
          identifierError: undefined,
          otpError: undefined,
        });
        this.startCountdown();
      },
      error: (err) => {
        this.clearCountdown();
        this.mapError(err, 'identifier');
      },
    });
  }

  onOtpSubmit(): void {
    if (!this.otpCode.trim()) {
      this.vm.update((v) => ({
        ...v,
        otpError: 'Enter the 6-digit OTP code.',
      }));
      return;
    }

    this.clearError();
    this.setLoading();

    const request: VerifyOtpRequest = {
      challengeId: this.challengeId,
      code: this.otpCode.trim(),
      normalizedIdentifier: this.normalizedIdentifier,
    };

    this.authService.verifyOtp(request).subscribe({
      next: (response) => {
        this.clearCountdown();
        this.store.setSession({
          accessToken: response.accessToken,
          refreshTokenHash: '',
          userId: response.accountId,
          deviceId: response.deviceId,
          sessionId: response.deviceId,
          persona: 'Employer',
          expiresOn: new Date(response.expiresOn),
        });
        this.vm.set({
          state: 'success',
          identifierType: this.vm().identifierType,
          message: 'Verification successful! Redirecting...',
          otpState: 'verified',
          resendDisabled: true,
          resendCountdownSeconds: 0,
          canSubmitIdentifier: true,
          canSubmitOtp: true,
          identifierError: undefined,
          otpError: undefined,
        });
        setTimeout(() => this.redirectToEmployer(), 1500);
      },
      error: (err) => {
        this.clearCountdown();
        this.mapError(err, 'otp');
      },
    });
  }

  onResendOtp(): void {
    if (this.vm().resendDisabled) return;

    this.clearError();
    this.setLoading();

    const type = this.vm().identifierType;
    const request: RequestOtpRequest = {
      accountId: this.accountId || this.generateAccountId(),
      purpose: 'Login',
      normalizedIdentifier: this.normalizedIdentifier,
      channel: type === 'mobile' ? 'Sms' : 'Email',
    };

    this.authService.requestOtp(request).subscribe({
      next: (response) => {
        this.challengeId = response.challengeId;
        this.accountId = request.accountId;
        const maskedTarget =
          type === 'mobile'
            ? this.maskMobile(this.normalizedIdentifier)
            : this.maskEmail(this.normalizedIdentifier);
        this.vm.update((v) => ({
          ...v,
          state: 'otp',
          otpState: 'sent',
          message: `OTP sent to ${maskedTarget}`,
          resendDisabled: true,
          resendCountdownSeconds: RESEND_COOLDOWN_SECONDS,
          otpError: undefined,
        }));
        this.startCountdown();
      },
      error: (err) => {
        this.clearCountdown();
        this.mapError(err, 'otp');
      },
    });
  }

  onOtpInput(value: string): void {
    this.otpCode = value.replace(/\D/g, '').slice(0, 6);
    this.vm.update((v) => ({
      ...v,
      otpError: undefined,
      canSubmitOtp: this.otpCode.length === 6,
    }));
  }

  retryFromError(): void {
    this.clearCountdown();
    this.vm.set({
      state: 'identifier',
      identifierType: this.vm().identifierType,
      otpState: 'idle',
      resendDisabled: true,
      resendCountdownSeconds: 0,
      canSubmitIdentifier: this.isValidIdentifier(),
      canSubmitOtp: false,
      message: undefined,
      errorCode: undefined,
      identifierError: undefined,
      otpError: undefined,
    });
  }

  // ── private helpers ────────────────────────────────────────────────────

  private isValidIdentifier(): boolean {
    const type = this.vm().identifierType;
    return type === 'mobile'
      ? MOBILE_REGEX.test(this.identifierValue)
      : EMAIL_REGEX.test(this.identifierValue);
  }

  private buildValidationError(): string {
    return this.vm().identifierType === 'mobile'
      ? 'Enter a valid 10-digit mobile number starting with 6, 7, 8, or 9.'
      : 'Enter a valid email address.';
  }

  private setLoading(): void {
    this.vm.update((v) => ({
      ...v,
      state: 'loading',
      canSubmitIdentifier: false,
      canSubmitOtp: false,
      identifierError: undefined,
      otpError: undefined,
    }));
  }

  private mapError(err: unknown, source: 'identifier' | 'otp'): void {
    const code = this.extractErrorCode(err);
    const message = this.mapErrorMessage(code, source);

    this.vm.update((v) => ({
      ...v,
      state: 'error',
      errorCode: code,
      message,
      canSubmitIdentifier: source === 'identifier',
      canSubmitOtp: source === 'otp',
      identifierError: source === 'identifier' ? message : undefined,
      otpError: source === 'otp' ? message : undefined,
    }));
  }

  private extractErrorCode(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const errObj = err as { error?: { Code?: string; code?: string } };
      return (errObj.error?.Code ?? errObj.error?.code ?? 'UNKNOWN') as string;
    }
    if (err && typeof err === 'object' && 'status' in err) {
      const statusErr = err as { status: number };
      return statusErr.status === 429 ? 'RATE_LIMITED' : 'UNKNOWN';
    }
    return 'UNKNOWN';
  }

  private mapErrorMessage(code: string, source: 'identifier' | 'otp'): string {
    const messages: Record<string, string> = {
      VALIDATION_FAILED:
        source === 'identifier'
          ? 'Invalid request. Please check your mobile number or email address.'
          : 'Invalid verification request. Please try again.',
      OTP_EXPIRED: 'This OTP has expired. Please request a new one.',
      OTP_INVALID:
        source === 'identifier'
          ? 'Unable to send OTP. Please try again.'
          : 'Invalid OTP code. Please check and try again.',
      OTP_ATTEMPTS_EXCEEDED: 'Too many incorrect attempts. Please request a new OTP.',
      RATE_LIMITED: 'Too many requests. Please wait before trying again.',
      FORBIDDEN: 'Access denied. Please try again.',
      UNAUTHENTICATED: 'Session expired. Please re-authenticate.',
      SESSION_REVOKED: 'Session revoked. Please log in again.',
      TOKEN_REUSE_DETECTED: 'Security alert: Token reuse detected. Please log in again.',
    };
    return messages[code] ?? `An error occurred. Please try again.`;
  }

  private startCountdown(): void {
    this.clearCountdown();
    this.countdownTimer = setInterval(() => {
      this.vm.update((v) => {
        const remaining = v.resendCountdownSeconds - 1;
        if (remaining <= 0) {
          this.clearCountdown();
          return { ...v, resendDisabled: false, resendCountdownSeconds: 0 };
        }
        return { ...v, resendCountdownSeconds: remaining };
      });
    }, 1000);
  }

  private clearCountdown(): void {
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
  }

  private clearError(): void {
    this.vm.update((v) => ({
      ...v,
      state: v.state === 'error' ? 'otp' : v.state,
      errorCode: undefined,
      message: undefined,
      identifierError: undefined,
      otpError: undefined,
    }));
  }

  private maskMobile(phone: string): string {
    if (phone.length < 4) return '***-****';
    return `***-***-${phone.slice(-4)}`;
  }

  private maskEmail(email: string): string {
    const [local, domain] = email.split('@');
    if (!local || !domain) return '***@***';
    const visible = local.length > 2 ? local.slice(0, 2) : local[0];
    return `${visible}***@${domain}`;
  }

  private generateAccountId(): string {
    return '00000000-0000-0000-0000-' + Math.random().toString(16).slice(2, 10).padEnd(12, '0');
  }

  private redirectToEmployer(): void {
    this.router.navigate(['/employer']);
  }
}
