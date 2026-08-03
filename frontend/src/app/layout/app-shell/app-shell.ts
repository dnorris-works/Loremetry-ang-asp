import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { SeriesService } from '../../core/series/series.service';
import { StoriesService } from '../../core/stories/stories.service';
import { AppFooter } from '../app-footer/app-footer';
import { AppHeader } from '../app-header/app-header';
import { AppSidebar } from '../app-sidebar/app-sidebar';
import { SeriesPanel } from '../series-panel/series-panel';
import { StoryPanel } from '../story-panel/story-panel';

const MIN_SIDEBAR_WIDTH = 180;
const MAX_SIDEBAR_WIDTH = 420;
const DEFAULT_SIDEBAR_WIDTH = 220;

@Component({
  selector: 'app-shell',
  imports: [AppHeader, AppSidebar, SeriesPanel, StoryPanel, AppFooter, RouterOutlet],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
})
export class AppShell {
  private readonly seriesService = inject(SeriesService);
  private readonly storiesService = inject(StoriesService);

  protected readonly isSeriesPanelOpen = this.seriesService.isPanelOpen;
  protected readonly isStoryPanelOpen = this.storiesService.isPanelOpen;
  protected readonly sidebarWidth = signal(DEFAULT_SIDEBAR_WIDTH);
  protected readonly isResizing = signal(false);

  protected readonly sidebarStyle = computed(() => ({
    width: `${this.sidebarWidth()}px`,
  }));

  protected onResizePointerDown(event: PointerEvent): void {
    event.preventDefault();

    const handle = event.currentTarget as HTMLElement;
    const startX = event.clientX;
    const startWidth = this.sidebarWidth();

    this.isResizing.set(true);
    handle.setPointerCapture(event.pointerId);

    const onPointerMove = (moveEvent: PointerEvent): void => {
      const nextWidth = startWidth + (moveEvent.clientX - startX);
      this.sidebarWidth.set(
        Math.min(MAX_SIDEBAR_WIDTH, Math.max(MIN_SIDEBAR_WIDTH, nextWidth)),
      );
    };

    const onPointerUp = (upEvent: PointerEvent): void => {
      this.isResizing.set(false);
      handle.releasePointerCapture(upEvent.pointerId);
      handle.removeEventListener('pointermove', onPointerMove);
      handle.removeEventListener('pointerup', onPointerUp);
      handle.removeEventListener('pointercancel', onPointerUp);
    };

    handle.addEventListener('pointermove', onPointerMove);
    handle.addEventListener('pointerup', onPointerUp);
    handle.addEventListener('pointercancel', onPointerUp);
  }

  protected onResizeKeyDown(event: KeyboardEvent): void {
    const step = event.shiftKey ? 32 : 16;

    switch (event.key) {
      case 'ArrowLeft':
        event.preventDefault();
        this.sidebarWidth.update((width) =>
          Math.max(MIN_SIDEBAR_WIDTH, width - step),
        );
        break;
      case 'ArrowRight':
        event.preventDefault();
        this.sidebarWidth.update((width) =>
          Math.min(MAX_SIDEBAR_WIDTH, width + step),
        );
        break;
      case 'Home':
        event.preventDefault();
        this.sidebarWidth.set(MIN_SIDEBAR_WIDTH);
        break;
      case 'End':
        event.preventDefault();
        this.sidebarWidth.set(MAX_SIDEBAR_WIDTH);
        break;
    }
  }
}
