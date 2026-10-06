import { Routes } from '@angular/router';
import { EmployerDashboardComponent } from './pages/employer-dashboard/employer-dashboard.component';
import { EmployerOtpLoginComponent } from './otp-login/employer-otp-login.component';
import { BuildEmployerTeamInvitationUiComponent } from './build-employer-team-invitation-ui/build-employer-team-invitation-ui.component';
import { BuildEmployerOrganisationSetupUiComponent } from './build-employer-organisation-setup-ui/build-employer-organisation-setup-ui.component';
import { authGuard, personaGuard } from '../../core/auth/auth.guards';

export const employerRoutes: Routes = [
  {
    path: 'otp-login',
    component: EmployerOtpLoginComponent,
    title: 'IndiaWorks — Employer OTP Login',
    // Public: intentionally not guarded.
  },
  {
    path: 'organisation-setup',
    component: BuildEmployerOrganisationSetupUiComponent,
    title: 'IndiaWorks — Organisation Setup',
    canActivate: [authGuard, personaGuard('Employer')],
  },
  {
    path: 'team-invitation',
    component: BuildEmployerTeamInvitationUiComponent,
    title: 'IndiaWorks — Team Invitation',
    canActivate: [authGuard, personaGuard('Employer')],
  },
  {
    path: '',
    component: EmployerDashboardComponent,
    title: 'IndiaWorks — Employer workspace',
    canActivate: [authGuard, personaGuard('Employer')],
  },
  {
    path: 'jobs',
    component: EmployerDashboardComponent,
    title: 'IndiaWorks — Employer jobs',
    canActivate: [authGuard, personaGuard('Employer')],
  },
];
