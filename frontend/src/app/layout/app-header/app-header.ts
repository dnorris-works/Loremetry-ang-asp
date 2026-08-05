import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { SeriesService } from '../../core/series/series.service';
import { StoriesService } from '../../core/stories/stories.service';
import { WritingService } from '../../core/writing/writing.service';

interface NavItem {
  label: string;
  route: string;
}

@Component({
  selector: 'app-header',
  imports: [RouterLink],
  templateUrl: './app-header.html',
  styleUrl: './app-header.css',
})
export class AppHeader {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly seriesService = inject(SeriesService);
  private readonly storiesService = inject(StoriesService);
  private readonly writingService = inject(WritingService);

  protected readonly appName = 'Loremetry';
  protected readonly isWriteMenuActive = computed(
    () => this.writingService.isPanelOpen() && !this.writingService.isDocumentMode(),
  );

  protected readonly navItems = computed<NavItem[]>(() => [
    { label: 'Dashboard', route: '/' },
    ...(this.auth.isAdmin() ? [{ label: 'Admin', route: '/admin' }] : []),
    { label: 'Settings', route: '/settings' },
  ]);

  protected readonly userLabel = computed(() => {
    const user = this.auth.me();
    if (!user) {
      return '';
    }

    const name = [user.firstName, user.lastName].filter(Boolean).join(' ').trim();
    return name || user.email;
  });

  protected onSignOut(): void {
    void this.auth.signOut();
  }

  protected isRouteNavActive(route: string): boolean {
    if (this.isWriteMenuActive()) {
      return false;
    }

    const path = this.router.url.split('?')[0].split('#')[0];
    if (route === '/') {
      return path === '/' || path === '';
    }

    return path === route || path.startsWith(`${route}/`);
  }

  protected onNavClick(event: MouseEvent, route: string): void {
    if (!this.writingService.isPanelOpen()) {
      return;
    }

    event.preventDefault();
    void this.navigateAway(route);
  }

  protected openWritingPanel(): void {
    void this.openWritingPanelAsync();
  }

  private async openWritingPanelAsync(): Promise<void> {
    this.seriesService.closePanel();
    this.storiesService.closePanel();
    this.writingService.openPanel();
  }

  private async navigateAway(route: string): Promise<void> {
    await this.writingService.closePanelAsync();
    await this.router.navigateByUrl(route);
  }
}
