import { Component, computed, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { StoriesService } from '../../core/stories/stories.service';

export interface SidebarItem {
  label: string;
  icon?: string;
  route: string;
}

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './app-sidebar.html',
  styleUrl: './app-sidebar.css',
})
export class AppSidebar {
  private readonly auth = inject(AuthService);
  private readonly storiesService = inject(StoriesService);

  readonly items = input<SidebarItem[] | null>(null);

  protected readonly navItems = computed(() => {
    if (this.items()) {
      return this.items()!;
    }

    return [
      { label: 'Dashboard', route: '/', icon: '◉' },
      ...(this.auth.isAdmin() ? [{ label: 'Admin', route: '/admin', icon: '⛭' }] : []),
      { label: 'Settings', route: '/settings', icon: '⚙' },
    ];
  });

  protected readonly stories = this.storiesService.stories;

  protected openAddStoryPanel(): void {
    this.storiesService.openAddPanel();
  }
}
