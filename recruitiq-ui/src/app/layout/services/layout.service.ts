import { inject, Injectable, signal } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly isSidebarCollapsed = signal<boolean>(false);
  readonly isMobile = signal<boolean>(false);
  readonly isTablet = signal<boolean>(false);

  constructor() {
    this.breakpointObserver.observe([
      Breakpoints.Handset,
      Breakpoints.Tablet
    ]).subscribe(() => {
      const isMobileView = this.breakpointObserver.isMatched(Breakpoints.Handset);
      const isTabletView = this.breakpointObserver.isMatched(Breakpoints.Tablet);

      this.isMobile.set(isMobileView);
      this.isTablet.set(isTabletView);

      // Mobile/Tablet collapsed by default, Desktop expanded
      if (isMobileView || isTabletView) {
        this.isSidebarCollapsed.set(true);
      } else {
        this.isSidebarCollapsed.set(false);
      }
    });
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed.update(collapsed => !collapsed);
  }
}
