import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'admin'
  },
  {
    path: 'auth',
    loadComponent: () => import('./layout/auth-layout/auth-layout.component').then(m => m.AuthLayoutComponent),
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/routes')
  },
  {
    path: 'admin',
    loadComponent: () => import('./layout/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    // For V1 scaffolding, we keep guard checks registered but bypass the blocking by setting auth state initially to true in test
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/routes')
      },
      {
        path: 'jobs',
        loadChildren: () => import('./features/jobs/routes')
      },
      {
        path: 'departments',
        loadChildren: () => import('./features/departments/routes')
      },
      {
        path: 'company-settings',
        loadChildren: () => import('./features/company-settings/routes')
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'admin'
  }
];
