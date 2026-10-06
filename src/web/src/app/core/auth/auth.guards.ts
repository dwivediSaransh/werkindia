import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSessionStore } from './auth-session.store';

/**
 * Auth guard: redirects unauthenticated users to the appropriate OTP login screen.
 * Preserves the attempted URL as a `returnUrl` query param.
 *
 * Usage in routes:
 *   canActivate: [authGuard]
 */
export const authGuard: CanActivateFn = (route, state) => {
  const store = inject(AuthSessionStore);
  const router = inject(Router);

  if (store.isAuthenticated()) {
    return true;
  }

  // Determine which login screen based on the attempted route prefix.
  const url = state.url;
  const loginPath = url.startsWith('/employer')
    ? '/employer/otp-login'
    : url.startsWith('/admin')
      ? '/candidate/otp-login' // admin uses same OTP flow until admin login story
      : '/candidate/otp-login';

  return router.createUrlTree([loginPath], {
    queryParams: { returnUrl: url },
    queryParamsHandling: 'merge',
  });
};

/**
 * Persona guard: ensures the authenticated session persona matches the expected value.
 * Redirects cross-persona navigations to the correct workspace root.
 *
 * Usage in routes:
 *   canActivate: [personaGuard('Candidate')]
 */
export const personaGuard =
  (expectedPersona: 'Candidate' | 'Employer' | 'Admin'): CanActivateFn =>
  (_route, _state) => {
    const store = inject(AuthSessionStore);
    const router = inject(Router);

    const persona = store.persona();

    if (persona === expectedPersona) {
      return true;
    }

    // Cross-persona redirect to the correct workspace root.
    const redirectPath =
      persona === 'Employer' ? '/employer' : persona === 'Admin' ? '/admin' : '/candidate/jobs';

    return router.createUrlTree([redirectPath]);
  };
