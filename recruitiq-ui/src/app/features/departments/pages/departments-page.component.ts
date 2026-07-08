import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-departments-page',
  standalone: true,
  template: `
    <h1>Departments</h1>
    <p>Manage your company departments here.</p>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DepartmentsPageComponent {}
