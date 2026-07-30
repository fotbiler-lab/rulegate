import { DOCUMENT } from '@angular/common';
import { computed, effect, inject, Injectable, signal } from '@angular/core';

interface LayoutState {
  readonly staticMenuDesktopInactive: boolean;
  readonly mobileMenuActive: boolean;
}

const THEME_STORAGE_KEY = 'rulegate-document-approval-theme';

@Injectable({ providedIn: 'root' })
export class LayoutService {
  private readonly document = inject(DOCUMENT);

  readonly darkMode = signal(false);
  readonly state = signal<LayoutState>({
    staticMenuDesktopInactive: false,
    mobileMenuActive: false,
  });

  readonly containerClass = computed(() => ({
    'layout-static-inactive': this.state().staticMenuDesktopInactive,
    'layout-mobile-active': this.state().mobileMenuActive,
  }));

  constructor() {
    this.darkMode.set(this.readDarkModePreference());

    effect(() => {
      this.document.documentElement.classList.toggle('app-dark', this.darkMode());
      this.document.body.classList.toggle('blocked-scroll', this.state().mobileMenuActive);
    });
  }

  toggleMenu(): void {
    if (window.innerWidth > 991) {
      this.state.update((state) => ({
        ...state,
        staticMenuDesktopInactive: !state.staticMenuDesktopInactive,
      }));
      return;
    }

    this.state.update((state) => ({
      ...state,
      mobileMenuActive: !state.mobileMenuActive,
    }));
  }

  closeMobileMenu(): void {
    this.state.update((state) => ({ ...state, mobileMenuActive: false }));
  }

  toggleDarkMode(): void {
    this.darkMode.update((darkMode) => {
      const nextDarkMode = !darkMode;
      this.writeDarkModePreference(nextDarkMode);
      return nextDarkMode;
    });
  }

  private readDarkModePreference(): boolean {
    try {
      return this.document.defaultView?.localStorage.getItem(THEME_STORAGE_KEY) === 'dark';
    } catch {
      return false;
    }
  }

  private writeDarkModePreference(darkMode: boolean): void {
    try {
      this.document.defaultView?.localStorage.setItem(
        THEME_STORAGE_KEY,
        darkMode ? 'dark' : 'light',
      );
    } catch {
      // Storage can be unavailable in restricted browser contexts; the in-memory theme still works.
    }
  }
}
