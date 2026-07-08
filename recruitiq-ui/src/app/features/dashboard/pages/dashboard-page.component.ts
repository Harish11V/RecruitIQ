import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  template: `
    <h1>Dashboard</h1>
    <p>Welcome to RecruitIQ ATS. Track your hiring pipeline here.</p>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardPageComponent {}
