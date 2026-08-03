import { Component, computed, inject } from '@angular/core';

import { AppSettingsService } from '../../core/settings/app-settings.service';
import { ThemeService } from '../../core/themes/theme.service';
import { ThemeId } from '../../core/themes/theme.models';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  protected readonly appSettings = inject(AppSettingsService);
  protected readonly themeService = inject(ThemeService);

  protected readonly selectedThemeDescription = computed(() => {
    const selectedId = this.themeService.selectedThemeId();
    return (
      this.themeService.themes.find((theme) => theme.id === selectedId)?.description ??
      'Choose a color theme for the workspace.'
    );
  });

  protected onThemeChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as ThemeId;
    this.themeService.setTheme(value);
  }
}
