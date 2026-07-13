import { Injectable, signal } from '@angular/core';
export type Theme = 'light' | 'dark';
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>((localStorage.getItem('operations-theme') as Theme) || 'light');
  constructor() {
    this.apply(this.theme());
  }
  toggle() {
    this.set(this.theme() === 'light' ? 'dark' : 'light');
  }
  set(value: Theme) {
    this.theme.set(value);
    localStorage.setItem('operations-theme', value);
    this.apply(value);
  }
  private apply(value: Theme) {
    document.documentElement.dataset['theme'] = value;
  }
}
