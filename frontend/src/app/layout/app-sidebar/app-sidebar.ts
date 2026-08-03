import { Component, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { SettingsService } from '../../core/settings/settings.service';

export type SidebarAction = 'settings';

export interface SidebarItem {
  label: string;
  icon?: string;
  route?: string;
  action?: SidebarAction;
}

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.css',
})
export class AppSidebar {
  private readonly settingsService = inject(SettingsService);

  readonly items = input<SidebarItem[]>([
    { label: 'Dashboard', route: '/', icon: '◉' },
    { label: 'Projects', route: '/projects', icon: '▣' },
    { label: 'Settings', action: 'settings', icon: '⚙' },
  ]);

  protected openSettings(): void {
    this.settingsService.open();
  }
}
