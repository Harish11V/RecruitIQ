import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class StorageService {
  getItem(key: string): string | null {
    try {
      return localStorage.getItem(key) || sessionStorage.getItem(key);
    } catch {
      return null;
    }
  }

  setItem(key: string, value: string, isPersistent: boolean): void {
    try {
      if (isPersistent) {
        localStorage.setItem(key, value);
        sessionStorage.removeItem(key);
      } else {
        sessionStorage.setItem(key, value);
        localStorage.removeItem(key);
      }
    } catch {
      // Ignored
    }
  }

  removeItem(key: string): void {
    try {
      localStorage.removeItem(key);
      sessionStorage.removeItem(key);
    } catch {
      // Ignored
    }
  }

  clear(): void {
    try {
      localStorage.clear();
      sessionStorage.clear();
    } catch {
      // Ignored
    }
  }
}
