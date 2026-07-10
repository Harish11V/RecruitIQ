import { Routes } from '@angular/router';
import { JobsPageComponent } from './pages/jobs-list/jobs-page.component';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: '',
    component: JobsPageComponent,
    data: { breadcrumb: 'Jobs' }
  },
  {
    path: 'new',
    loadComponent: () => import('./pages/create-job/create-job-page.component').then(m => m.CreateJobPageComponent),
    canDeactivate: [unsavedChangesGuard],
    data: { breadcrumb: 'Create Job' }
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/job-details/job-details-page.component').then(m => m.JobDetailsPageComponent),
    data: { breadcrumb: 'Job Details' }
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./pages/edit-job/edit-job-page.component').then(m => m.EditJobPageComponent),
    canDeactivate: [unsavedChangesGuard],
    data: { breadcrumb: 'Edit Job' }
  }
];
export default routes;
