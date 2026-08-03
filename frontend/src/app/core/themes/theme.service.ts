import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

import { APP_THEMES, ThemeId } from './theme.models';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly prefersDark = signal(this.readSystemPrefersDark());

  readonly themes = APP_THEMES;
  readonly selectedThemeId = signal<ThemeId>('light');

  constructor() {
    const mediaQuery = this.document.defaultView?.matchMedia(
      '(prefers-color-scheme: dark)',
    );

    mediaQuery?.addEventListener('change', (event) => {
      this.prefersDark.set(event.matches);
    });

    effect(() => {
      const selected = this.selectedThemeId();
      const resolvedId = this.resolveThemeId(selected);
      const theme =
        APP_THEMES.find((item) => item.id === resolvedId) ?? APP_THEMES[0];

      this.document.documentElement.dataset['theme'] = resolvedId;
      this.document.documentElement.style.colorScheme = theme.colorScheme;
    });
  }

  setTheme(themeId: ThemeId): void {
    this.selectedThemeId.set(themeId);
  }

  private resolveThemeId(themeId: ThemeId): Exclude<ThemeId, 'system'> {
    if (themeId !== 'system') {
      return themeId;
    }

    return this.prefersDark() ? 'dark' : 'light';
  }

  private readSystemPrefersDark(): boolean {
    return (
      this.document.defaultView?.matchMedia('(prefers-color-scheme: dark)')
        .matches ?? false
    );
  }
}
