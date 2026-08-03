import { Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

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
  readonly items = input<SidebarItem[]>([
    { label: 'Dashboard', route: '/', icon: '◉' },
    { label: 'Admin', route: '/admin', icon: '⛭' },
    { label: 'Settings', route: '/settings', icon: '⚙' },
  ]);
}
