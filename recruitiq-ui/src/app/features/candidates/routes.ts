import { Routes } from '@angular/router';
import { CandidatesListPageComponent } from './pages/candidates-list/candidates-list-page.component';
import { CandidateDetailsPageComponent } from './pages/candidate-details/candidate-details-page.component';
import { CreateCandidatePageComponent } from './pages/create-candidate/create-candidate-page.component';
import { EditCandidatePageComponent } from './pages/edit-candidate/edit-candidate-page.component';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: '',
    component: CandidatesListPageComponent,
    data: { breadcrumb: 'Candidates' }
  },
  {
    path: 'create',
    component: CreateCandidatePageComponent,
    canDeactivate: [unsavedChangesGuard],
    data: { breadcrumb: 'Create Candidate' }
  },
  {
    path: ':id/edit',
    component: EditCandidatePageComponent,
    canDeactivate: [unsavedChangesGuard],
    data: { breadcrumb: 'Edit' }
  },
  {
    path: ':id',
    component: CandidateDetailsPageComponent,
    data: { breadcrumb: 'Candidate Details' }
  }
];
export default routes;
