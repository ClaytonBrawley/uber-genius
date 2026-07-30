import { Injectable, effect, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark';

const STORAGE_KEY = 'theme-preference';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<ThemePreference>(readInitialTheme());

  constructor() {
    // Keeps the `dark` class on <html> (which drives every dark: utility)
    // and localStorage in sync with the signal, on init and every toggle.
    effect(() => {
      const value = this.theme();
      document.documentElement.classList.toggle('dark', value === 'dark');
      localStorage.setItem(STORAGE_KEY, value);
    });
  }

  toggle(): void {
    this.theme.set(this.theme() === 'dark' ? 'light' : 'dark');
  }
}

function readInitialTheme(): ThemePreference {
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored === 'light' || stored === 'dark') {
    return stored;
  }

  // matchMedia isn't implemented in the jsdom test environment.
  if (typeof window.matchMedia !== 'function') {
    return 'light';
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}
