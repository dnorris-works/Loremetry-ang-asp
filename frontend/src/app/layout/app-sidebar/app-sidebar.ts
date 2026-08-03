import { Component, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

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
  private readonly storiesService = inject(StoriesService);

  readonly items = input<SidebarItem[]>([
    { label: 'Dashboard', route: '/', icon: '◉' },
    { label: 'Admin', route: '/admin', icon: '⛭' },
    { label: 'Settings', route: '/settings', icon: '⚙' },
  ]);

  protected readonly stories = this.storiesService.stories;

  protected openAddStoryPanel(): void {
    this.storiesService.openAddPanel();
  }
}
