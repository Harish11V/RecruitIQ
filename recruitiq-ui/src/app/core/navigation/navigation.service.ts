import { Injectable, signal } from '@angular/core';
import { NavigationSection } from './navigation.model';

@Injectable({
  providedIn: 'root'
})
export class NavigationService {
  readonly sections = signal<NavigationSection[]>([
    {
      items: [
        { label: 'Dashboard', route: '/admin/dashboard', icon: 'dashboard', enabled: true }
      ]
    },
    {
      sectionTitle: 'Management',
      items: [
        { label: 'Jobs', route: '/admin/jobs', icon: 'work', enabled: true },
        { label: 'Departments', route: '/admin/departments', icon: 'business', enabled: true },
        { label: 'Company Settings', route: '/admin/company-settings', icon: 'settings', enabled: true }
      ]
    },
    {
      sectionTitle: 'Recruitment',
      items: [
        { label: 'Candidates', route: '/admin/candidates', icon: 'people', enabled: false, badge: 'Coming Soon' },
        { label: 'Applications', route: '/admin/applications', icon: 'assignment', enabled: false, badge: 'Coming Soon' },
        { label: 'Interviews', route: '/admin/interviews', icon: 'event', enabled: false, badge: 'Coming Soon' }
      ]
    },
    {
      sectionTitle: 'AI Core',
      items: [
        { label: 'Resume Parser', route: '/admin/resume-parser', icon: 'psychology', enabled: false, badge: 'Coming Soon' },
        { label: 'Candidate Ranking', route: '/admin/candidate-ranking', icon: 'insights', enabled: false, badge: 'Coming Soon' }
      ]
    },
    {
      sectionTitle: 'Administration',
      items: [
        { label: 'Users', route: '/admin/users', icon: 'manage_accounts', enabled: false, badge: 'Coming Soon' },
        { label: 'Reports', route: '/admin/reports', icon: 'assessment', enabled: false, badge: 'Coming Soon' }
      ]
    },
    {
      sectionTitle: 'Settings',
      items: [
        { label: 'Settings', route: '/admin/settings', icon: 'settings_applications', enabled: false, badge: 'Coming Soon' }
      ]
    }
  ]);
}
