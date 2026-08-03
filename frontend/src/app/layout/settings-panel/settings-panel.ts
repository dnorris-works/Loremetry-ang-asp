import { Component, inject } from '@angular/core';

import { AppSettingsService } from '../../core/settings/app-settings.service';
import { SettingsService } from '../../core/settings/settings.service';
import { ThemeService } from '../../core/themes/theme.service';
import { ThemeId } from '../../core/themes/theme.models';

@Component({
  selector: 'app-settings-panel',
  templateUrl: './settings-panel.html',
  styleUrl: './settings-panel.css',
})
export class SettingsPanel {
  private readonly settingsService = inject(SettingsService);
  protected readonly appSettings = inject(AppSettingsService);
  protected readonly themeService = inject(ThemeService);

  protected readonly isOpen = this.settingsService.isOpen;

  protected close(): void {
    this.settingsService.close();
  }

  protected onBackdropClick(): void {
    this.close();
  }

  protected onPanelClick(event: MouseEvent): void {
    event.stopPropagation();
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
    }
  }

  protected selectTheme(themeId: ThemeId): void {
    this.themeService.setTheme(themeId);
  }

  protected isSelected(themeId: ThemeId): boolean {
    return this.themeService.selectedThemeId() === themeId;
  }
}
