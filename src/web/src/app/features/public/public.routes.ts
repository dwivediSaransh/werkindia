import { Routes } from '@angular/router';
import { PublicJobComponent } from './pages/public-job/public-job.component';
import { NotFoundComponent } from '../../shared/pages/not-found/not-found.component';

export const publicRoutes: Routes = [
  {
    path: 'j/:id',
    component: PublicJobComponent,
    title: 'IndiaWorks — Shared job',
    data: { renderMode: 'client' as const },
  },
  {
    path: '',
    redirectTo: 'j/sample-job',
    pathMatch: 'full',
  },
  {
    path: '**',
    component: NotFoundComponent,
    title: 'IndiaWorks — Page not found',
  },
];
