import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthSessionStore } from './auth-session.store';

export interface RefreshRequest {
  userId: string;
  deviceId: string;
  refreshTokenHash: string;
}

export interface RefreshResponse {
  accessToken: string;
  newRefreshTokenHash: string;
  expiresOn: string;
}

export interface LogoutRequest {
  userId: string;
  deviceId: string;
  refreshTokenHash: string;
}

export interface LogoutResponse {
  success: boolean;
}

export interface RequestOtpRequest {
  accountId: string;
  purpose: string;
  normalizedIdentifier: string;
  channel: string;
}

export interface RequestOtpResponse {
  challengeId: string;
  purpose: string;
  expiresOn: string;
  maxAttempts: number;
  resendCooldownSeconds: number;
}

export interface VerifyOtpRequest {
  challengeId: string;
  code: string;
  normalizedIdentifier: string;
}

export interface VerifyOtpResponse {
  accountId: string;
  displayName: string;
  provider: string;
  isNewAccount: boolean;
  deviceId: string;
  accessToken: string;
  expiresOn: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(AuthSessionStore);
  private readonly baseUrl = '/api/auth/otp';

  refresh(request: RefreshRequest): Observable<RefreshResponse> {
    return this.http.post<RefreshResponse>(`${this.baseUrl}/refresh`, request).pipe(
      tap((response) => {
        this.store.updateSession({
          accessToken: response.accessToken,
          refreshTokenHash: response.newRefreshTokenHash,
          expiresOn: new Date(response.expiresOn),
        });
      }),
    );
  }

  logout(request: LogoutRequest): Observable<LogoutResponse> {
    return this.http.post<LogoutResponse>(`${this.baseUrl}/logout`, request).pipe(
      tap(() => {
        this.store.clearSession();
      }),
    );
  }

  clearSession(): void {
    this.store.clearSession();
  }

  /** Candidate login flow uses the phone-number OTP endpoints owned by IW8-022/IW8-028. */
  requestOtp(request: RequestOtpRequest): Observable<RequestOtpResponse> {
    return this.http.post<RequestOtpResponse>(`${this.baseUrl}/request`, request);
  }

  verifyOtp(request: VerifyOtpRequest): Observable<VerifyOtpResponse> {
    return this.http.post<VerifyOtpResponse>(`${this.baseUrl}/verify`, request);
  }
}
