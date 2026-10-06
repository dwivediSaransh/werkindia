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
import {
  AuthService,
  RequestOtpRequest,
  RequestOtpResponse,
  VerifyOtpRequest,
  VerifyOtpResponse,
} from '../../../core/auth/auth.service';
import { AuthSessionStore } from '../../../core/auth/auth-session.store';

type ViewState = 'idle' | 'phone' | 'otp' | 'loading' | 'error' | 'success';
type OtpState = 'idle' | 'sent' | 'verified';

interface ViewModel {
  state: ViewState;
  message?: string;
  errorCode?: string;
  phoneError?: string;
  otpError?: string;
  otpState: OtpState;
  resendDisabled: boolean;
  resendCountdownSeconds: number;
  canSubmitPhone: boolean;
  canSubmitOtp: boolean;
}

const RESEND_COOLDOWN_SECONDS = 30;
const PHONE_REGEX = /^[6-9]\d{9}$/;

@Component({
  selector: 'app-build-candidate-mobile-otp-login-screen',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './build-candidate-mobile-otp-login-screen.component.html',
  styleUrl: './build-candidate-mobile-otp-login-screen.component.scss',
})
export class BuildCandidateMobileOtpLoginScreenComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly store = inject(AuthSessionStore);

  readonly vm = signal<ViewModel>({
    state: 'phone',
    otpState: 'idle',
    resendDisabled: true,
    resendCountdownSeconds: 0,
    canSubmitPhone: true,
    canSubmitOtp: true,
  });

  private phoneNumber = '';
  private otpCode = '';
  private challengeId = '';
  private normalizedIdentifier = '';
  private accountId = '';
  private countdownTimer: ReturnType<typeof setInterval> | null = null;

  // Expose for template binding (Angular template needs public access)
  readonly phoneInputValue = () => this.phoneNumber;
  readonly otpInputValue = () => this.otpCode;

  readonly viewState = computed(() => this.vm().state);
  readonly showPhoneStep = computed(() => this.vm().state === 'phone');
  readonly showOtpStep = computed(() => this.vm().state === 'otp');
  readonly showLoading = computed(() => this.vm().state === 'loading');
  readonly showError = computed(() => this.vm().state === 'error');
  readonly showSuccess = computed(() => this.vm().state === 'success');
  readonly errorMessage = computed(() => this.vm().message);
  readonly errorCode = computed(() => this.vm().errorCode);

  ngOnInit(): void {
    // If already authenticated, redirect to jobs
    if (this.store.isAuthenticated()) {
      this.redirectToJobs();
    }
  }

  ngOnDestroy(): void {
    this.clearCountdown();
  }

  onPhoneSubmit(): void {
    const trimmed = this.phoneNumber.trim();

    if (!PHONE_REGEX.test(trimmed)) {
      this.vm.update((v) => ({
        ...v,
        state: 'error',
        phoneError: 'Enter a valid 10-digit mobile number starting with 6, 7, 8, or 9.',
      }));
      return;
    }

    this.normalizedIdentifier = trimmed;
    this.clearError();
    this.setLoading();

    const request: RequestOtpRequest = {
      accountId: this.generateAccountId(),
      purpose: 'Login',
      normalizedIdentifier: trimmed,
      channel: 'Sms',
    };

    this.authService.requestOtp(request).subscribe({
      next: (response) => {
        this.challengeId = response.challengeId;
        this.accountId = request.accountId;
        this.vm.set({
          state: 'otp',
          otpState: 'sent',
          message: `OTP sent to ${this.maskPhone(trimmed)}`,
          resendDisabled: true,
          resendCountdownSeconds: RESEND_COOLDOWN_SECONDS,
          canSubmitPhone: true,
          canSubmitOtp: true,
          phoneError: undefined,
          otpError: undefined,
        });
        this.startCountdown();
      },
      error: (err) => {
        this.clearCountdown();
        this.mapError(err, 'phone');
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
          refreshTokenHash: '', // Set by refresh flow
          userId: response.accountId,
          deviceId: response.deviceId,
          sessionId: response.deviceId, // Use deviceId as session identifier
          persona: 'Candidate',
          expiresOn: new Date(response.expiresOn),
        });
        this.vm.set({
          state: 'success',
          message: 'Verification successful! Redirecting...',
          otpState: 'verified',
          resendDisabled: true,
          resendCountdownSeconds: 0,
          canSubmitPhone: true,
          canSubmitOtp: true,
          phoneError: undefined,
          otpError: undefined,
        });
        // Redirect after short delay for user feedback
        setTimeout(() => this.redirectToJobs(), 1500);
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

    const request: RequestOtpRequest = {
      accountId: this.accountId || this.generateAccountId(),
      purpose: 'Login',
      normalizedIdentifier: this.normalizedIdentifier,
      channel: 'Sms',
    };

    this.authService.requestOtp(request).subscribe({
      next: (response) => {
        this.challengeId = response.challengeId;
        this.accountId = request.accountId;
        this.vm.update((v) => ({
          ...v,
          otpState: 'sent',
          message: `OTP sent to ${this.maskPhone(this.normalizedIdentifier)}`,
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

  onPhoneInput(value: string): void {
    this.phoneNumber = value.replace(/\D/g, '').slice(0, 10);
    this.vm.update((v) => ({
      ...v,
      phoneError: undefined,
      canSubmitPhone: this.phoneNumber.length === 10 && PHONE_REGEX.test(this.phoneNumber),
    }));
  }

  onOtpInput(value: string): void {
    this.otpCode = value.replace(/\D/g, '').slice(0, 6);
    this.vm.update((v) => ({
      ...v,
      otpError: undefined,
      canSubmitOtp: this.otpCode.length === 6,
    }));
  }

  private setLoading(): void {
    this.vm.update((v) => ({
      ...v,
      state: 'loading',
      canSubmitPhone: false,
      canSubmitOtp: false,
      phoneError: undefined,
      otpError: undefined,
    }));
  }

  private mapError(err: unknown, source: 'phone' | 'otp'): void {
    const code = this.extractErrorCode(err);
    const message = this.mapErrorMessage(code, source);

    this.vm.update((v) => ({
      ...v,
      state: 'error',
      errorCode: code,
      message,
      canSubmitPhone: source === 'phone',
      canSubmitOtp: source === 'otp',
      phoneError: source === 'phone' ? message : undefined,
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

  private mapErrorMessage(code: string, source: 'phone' | 'otp'): string {
    const messages: Record<string, string> = {
      VALIDATION_FAILED:
        source === 'phone'
          ? 'Invalid request. Please check your mobile number.'
          : 'Invalid verification request. Please try again.',
      OTP_EXPIRED: 'This OTP has expired. Please request a new one.',
      OTP_INVALID:
        source === 'phone'
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
      phoneError: undefined,
      otpError: undefined,
    }));
  }

  retryFromError(): void {
    this.clearCountdown();
    this.vm.set({
      state: 'phone',
      otpState: 'idle',
      resendDisabled: true,
      resendCountdownSeconds: 0,
      canSubmitPhone: this.phoneNumber.length === 10 && PHONE_REGEX.test(this.phoneNumber),
      canSubmitOtp: true,
      message: undefined,
      errorCode: undefined,
      phoneError: undefined,
      otpError: undefined,
    });
  }

  private maskPhone(phone: string): string {
    if (phone.length < 4) return '***-****';
    return `***-***-${phone.slice(-4)}`;
  }

  private generateAccountId(): string {
    // Generate a deterministic-looking GUID for demo purposes
    return '00000000-0000-0000-0000-' + Math.random().toString(16).slice(2, 10).padEnd(12, '0');
  }

  private redirectToJobs(): void {
    this.router.navigate(['/candidate/jobs']);
  }
}
