import { inject, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DialogService {
  private readonly dialog = inject(MatDialog);

  confirm(title: string, message: string): Observable<boolean> {
    // Base skeleton returning JS confirm fallback or placeholder observable
    try {
      const result = confirm(`${title}: ${message}`);
      return of(result);
    } catch {
      return of(true);
    }
  }
}
