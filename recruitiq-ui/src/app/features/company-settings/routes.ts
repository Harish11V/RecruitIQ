import { Routes } from '@angular/router';
import { CompanySettingsPageComponent } from './pages/company-settings-page.component';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

export const routes: Routes = [
  {
    path: '',
    component: CompanySettingsPageComponent,
    canDeactivate: [unsavedChangesGuard],
    data: { breadcrumb: 'Company Settings' }
  }
];
export default routes;
