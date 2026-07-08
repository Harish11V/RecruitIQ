import { Routes } from '@angular/router';
import { CompanySettingsPageComponent } from './pages/company-settings-page.component';

export const routes: Routes = [
  {
    path: '',
    component: CompanySettingsPageComponent,
    data: { breadcrumb: 'Company Settings' }
  }
];
export default routes;
