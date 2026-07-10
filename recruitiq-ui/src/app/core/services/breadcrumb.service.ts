import { inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

export interface Breadcrumb {
  label: string;
  url: string;
}

@Injectable({
  providedIn: 'root'
})
export class BreadcrumbService {
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);

  readonly breadcrumbs = signal<Breadcrumb[]>([]);

  constructor() {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      const root = this.activatedRoute.root;
      this.breadcrumbs.set(this.createBreadcrumbs(root));
    });
  }

  private createBreadcrumbs(route: ActivatedRoute, url = '', breadcrumbs: Breadcrumb[] = []): Breadcrumb[] {
    const children: ActivatedRoute[] = route.children;

    if (children.length === 0) {
      return breadcrumbs;
    }

    for (const child of children) {
      const routeURL: string = child.snapshot.url.map(segment => segment.path).join('/');
      let nextUrl = url;
      if (routeURL !== '') {
        nextUrl += `/${routeURL}`;
      }

      const label = child.snapshot.data['breadcrumb'];
      if (label) {
        breadcrumbs.push({ label, url: nextUrl });
      }

      return this.createBreadcrumbs(child, nextUrl, breadcrumbs);
    }

    return breadcrumbs;
  }

  updateBreadcrumbLabel(url: string, label: string): void {
    const current = this.breadcrumbs();
    const updated = current.map(bc => {
      if (bc.url === url || bc.url.endsWith(url)) {
        return { ...bc, label };
      }
      return bc;
    });
    this.breadcrumbs.set(updated);
  }
}
