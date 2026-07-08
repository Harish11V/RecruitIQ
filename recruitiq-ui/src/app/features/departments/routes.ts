import { Routes } from '@angular/router';
import { DepartmentsPageComponent } from './pages/departments-page.component';

export const routes: Routes = [
  {
    path: '',
    component: DepartmentsPageComponent,
    data: { breadcrumb: 'Departments' }
  }
];
export default routes;
