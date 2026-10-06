import { Routes } from '@angular/router';
import { NotFoundComponent } from './shared/pages/not-found/not-found.component';

export const routes: Routes = [
  {
    path: 'candidate',
    loadChildren: () =>
      import('./features/candidate/candidate.routes').then((m) => m.candidateRoutes),
  },
  {
    path: 'employer',
    loadChildren: () => import('./features/employer/employer.routes').then((m) => m.employerRoutes),
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.adminRoutes),
  },
  {
    path: 'share',
    loadChildren: () => import('./features/public/public.routes').then((m) => m.publicRoutes),
  },
  {
    path: '',
    redirectTo: 'candidate/jobs',
    pathMatch: 'full',
  },
  {
    path: '**',
    component: NotFoundComponent,
    title: 'IndiaWorks — Page not found',
  },
];
