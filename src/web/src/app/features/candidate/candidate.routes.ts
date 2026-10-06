import { Routes } from '@angular/router';
import { JobPortalComponent } from '../job-portal/job-portal.component';
import { JobDetailComponent } from './pages/job-detail/job-detail.component';
import { BuildCandidateMobileOtpLoginScreenComponent } from './build-candidate-mobile-otp-login-screen/build-candidate-mobile-otp-login-screen.component';
import { authGuard, personaGuard } from '../../core/auth/auth.guards';

export const candidateRoutes: Routes = [
  {
    path: 'otp-login',
    component: BuildCandidateMobileOtpLoginScreenComponent,
    title: 'IndiaWorks — Candidate OTP Login',
    // Public: intentionally not guarded — unauthenticated users must reach this.
  },
  {
    path: 'jobs',
    component: JobPortalComponent,
    title: 'IndiaWorks — Job search',
    canActivate: [authGuard, personaGuard('Candidate')],
  },
  {
    path: 'job/:id',
    component: JobDetailComponent,
    title: 'IndiaWorks — Job detail',
    data: { renderMode: 'client' as const },
    canActivate: [authGuard, personaGuard('Candidate')],
  },
  {
    path: '',
    redirectTo: 'jobs',
    pathMatch: 'full',
  },
];
