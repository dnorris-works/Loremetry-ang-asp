import {
  Component,
  ElementRef,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';

import { Series } from '../../core/series/series.models';
import { Story } from '../../core/stories/story.models';
import { StoriesService } from '../../core/stories/stories.service';

@Component({
  selector: 'app-add-story-to-series-dialog',
  templateUrl: './add-story-to-series-dialog.html',
  styleUrl: './add-story-to-series-dialog.css',
})
export class AddStoryToSeriesDialog {
  private readonly storiesService = inject(StoriesService);

  readonly story = input.required<Story>();
  readonly series = input.required<Series[]>();
  readonly closed = output<void>();

  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isSaving = signal(false);

  private readonly dialog = viewChild<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const element = this.dialog()?.nativeElement;
      if (!element) {
        return;
      }

      if (!element.open) {
        element.showModal();
      }
    });
  }

  protected cancel(): void {
    this.close();
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === this.dialog()?.nativeElement) {
      this.close();
    }
  }

  protected async selectSeries(seriesId: number): Promise<void> {
    if (this.isSaving()) {
      return;
    }

    this.errorMessage.set(null);
    this.isSaving.set(true);

    const updated = await this.storiesService.assignToSeries(this.story().id, seriesId);

    this.isSaving.set(false);

    if (updated) {
      this.close();
      return;
    }

    this.errorMessage.set(this.storiesService.saveError() ?? 'Failed to add story to series.');
  }

  private close(): void {
    this.dialog()?.nativeElement.close();
    this.closed.emit();
  }
}
