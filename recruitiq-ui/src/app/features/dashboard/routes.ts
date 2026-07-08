import { Routes } from '@angular/router';
import { DashboardPageComponent } from './pages/dashboard-page.component';

export const routes: Routes = [
  {
    path: '',
    component: DashboardPageComponent,
    data: { breadcrumb: 'Dashboard' }
  }
];
export default routes;
