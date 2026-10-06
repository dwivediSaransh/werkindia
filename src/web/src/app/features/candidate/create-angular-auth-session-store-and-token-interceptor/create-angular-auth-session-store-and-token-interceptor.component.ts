import { Component, signal, ChangeDetectionStrategy, inject } from '@angular/core';
import { AuthSessionStore } from '../../../core/auth/auth-session.store';
import { AuthService } from '../../../core/auth/auth.service';

interface ViewModel {
  state: 'loading' | 'authenticated' | 'error' | 'idle';
  message?: string;
  persona?: string | null;
  userId?: string | null;
}

@Component({
  selector: 'app-create-angular-auth-session-store-and-token-interceptor',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './create-angular-auth-session-store-and-token-interceptor.component.html',
  styleUrl: './create-angular-auth-session-store-and-token-interceptor.component.scss',
})
export class CreateAngularAuthSessionStoreAndTokenInterceptorComponent {
  private readonly store = inject(AuthSessionStore);
  private readonly authService = inject(AuthService);

  readonly vm = signal<ViewModel>({ state: 'idle' });
  refreshInProgress = false;

  get isAuthenticated(): boolean {
    return this.store.isAuthenticated();
  }

  get persona(): string | null {
    return this.store.persona();
  }

  get userId(): string | null {
    return this.store.userId();
  }

  get isRefreshDisabled(): boolean {
    return !this.isAuthenticated || this.refreshInProgress;
  }

  setMockSession(): void {
    this.store.setSession({
      accessToken: 'mock-access-token-' + Date.now(),
      refreshTokenHash: 'mock-refresh-hash-' + Date.now(),
      userId: 'user-' + Date.now(),
      deviceId: 'device-' + Date.now(),
      sessionId: 'session-' + Date.now(),
      persona: 'Candidate',
      expiresOn: new Date(Date.now() + 3600_000),
    });
    this.vm.set({
      state: 'authenticated',
      message: 'Mock session set successfully',
      persona: 'Candidate',
      userId: this.store.userId(),
    });
  }

  triggerRefresh(): void {
    if (this.refreshInProgress) {
      this.vm.set({
        state: 'loading',
        message: 'Refresh already in progress...',
      });
      return;
    }

    const userId = this.store.userId();
    const deviceId = this.store.deviceId();
    const refreshTokenHash = this.store.refreshTokenHash();

    if (!userId || !deviceId || !refreshTokenHash) {
      this.vm.set({
        state: 'error',
        message: 'No valid session to refresh. Please set a mock session first.',
      });
      return;
    }

    this.refreshInProgress = true;
    this.vm.set({ state: 'loading', message: 'Refreshing token...' });

    this.authService
      .refresh({
        userId,
        deviceId,
        refreshTokenHash,
      })
      .subscribe({
        next: (response) => {
          this.store.updateSession({
            accessToken: response.accessToken,
            refreshTokenHash: response.newRefreshTokenHash,
            expiresOn: new Date(response.expiresOn),
          });
          this.refreshInProgress = false;
          this.vm.set({
            state: 'authenticated',
            message: 'Token refreshed successfully',
            persona: this.persona,
            userId: this.userId,
          });
        },
        error: (err) => {
          this.refreshInProgress = false;
          this.store.clearSession();
          this.vm.set({
            state: 'error',
            message:
              'Refresh failed: ' +
              (err.error?.Code ?? 'UNKNOWN') +
              ' - ' +
              (err.error?.Message ?? 'Please re-authenticate'),
          });
        },
      });
  }

  logout(): void {
    const userId = this.store.userId();
    const deviceId = this.store.deviceId();
    const refreshTokenHash = this.store.refreshTokenHash();

    if (!userId || !deviceId || !refreshTokenHash) {
      this.vm.set({
        state: 'error',
        message: 'No valid session to logout.',
      });
      return;
    }

    this.authService
      .logout({
        userId,
        deviceId,
        refreshTokenHash,
      })
      .subscribe({
        next: () => {
          this.store.clearSession();
          this.vm.set({
            state: 'idle',
            message: 'Logged out successfully',
          });
        },
        error: () => {
          this.store.clearSession();
          this.vm.set({
            state: 'idle',
            message: 'Logged out (session cleared)',
          });
        },
      });
  }
}
