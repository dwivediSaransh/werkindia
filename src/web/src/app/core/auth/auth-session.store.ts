import { Injectable, signal, computed } from '@angular/core';

/**
 * Signals-based auth session store.
 * Holds session state without logging token values.
 */
export interface AuthSession {
  accessToken: string | null;
  refreshTokenHash: string | null;
  userId: string | null;
  deviceId: string | null;
  sessionId: string | null;
  persona: string | null;
  expiresOn: Date | null;
}

@Injectable({ providedIn: 'root' })
export class AuthSessionStore {
  private readonly _accessToken = signal<string | null>(null);
  private readonly _refreshTokenHash = signal<string | null>(null);
  private readonly _userId = signal<string | null>(null);
  private readonly _deviceId = signal<string | null>(null);
  private readonly _sessionId = signal<string | null>(null);
  private readonly _persona = signal<string | null>(null);
  private readonly _expiresOn = signal<Date | null>(null);

  readonly accessToken = this._accessToken.asReadonly();
  readonly refreshTokenHash = this._refreshTokenHash.asReadonly();
  readonly userId = this._userId.asReadonly();
  readonly deviceId = this._deviceId.asReadonly();
  readonly sessionId = this._sessionId.asReadonly();
  readonly persona = this._persona.asReadonly();
  readonly expiresOn = this._expiresOn.asReadonly();

  readonly isAuthenticated = computed(() => {
    const token = this._accessToken();
    return token !== null && token !== '';
  });

  readonly sessionExpiresSoon = computed(() => {
    const expires = this._expiresOn();
    if (!expires) return false;
    const buffer = 30_000; // 30 seconds
    return expires.getTime() - Date.now() < buffer;
  });

  setSession(session: AuthSession): void {
    this._accessToken.set(session.accessToken);
    this._refreshTokenHash.set(session.refreshTokenHash);
    this._userId.set(session.userId);
    this._deviceId.set(session.deviceId);
    this._sessionId.set(session.sessionId);
    this._persona.set(session.persona);
    this._expiresOn.set(session.expiresOn);
  }

  clearSession(): void {
    this._accessToken.set(null);
    this._refreshTokenHash.set(null);
    this._userId.set(null);
    this._deviceId.set(null);
    this._sessionId.set(null);
    this._persona.set(null);
    this._expiresOn.set(null);
  }

  updateSession(partial: Partial<AuthSession>): void {
    if (partial.accessToken !== undefined) this._accessToken.set(partial.accessToken);
    if (partial.refreshTokenHash !== undefined)
      this._refreshTokenHash.set(partial.refreshTokenHash);
    if (partial.userId !== undefined) this._userId.set(partial.userId);
    if (partial.deviceId !== undefined) this._deviceId.set(partial.deviceId);
    if (partial.sessionId !== undefined) this._sessionId.set(partial.sessionId);
    if (partial.persona !== undefined) this._persona.set(partial.persona);
    if (partial.expiresOn !== undefined) this._expiresOn.set(partial.expiresOn);
  }
}
