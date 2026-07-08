import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-company-settings-page',
  standalone: true,
  template: `
    <h1>Company Settings</h1>
    <p>Manage company details, visual appearance, and regional timezone preferences here.</p>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompanySettingsPageComponent {}
