import { Component, inject, signal } from '@angular/core';

import { SeriesService } from '../../core/series/series.service';

@Component({
  selector: 'app-series-panel',
  templateUrl: './series-panel.html',
  styleUrl: './series-panel.css',
})
export class SeriesPanel {
  private readonly seriesService = inject(SeriesService);

  protected readonly seriesName = signal('');
  protected readonly errorMessage = signal<string | null>(null);

  protected close(): void {
    this.resetForm();
    this.seriesService.closeAddPanel();
  }

  protected save(): void {
    this.errorMessage.set(null);

    const name = this.seriesName().trim();
    if (!name) {
      this.errorMessage.set('Series name is required.');
      return;
    }

    this.seriesService.addSeries({ name });
    this.resetForm();
  }

  protected onSeriesNameInput(event: Event): void {
    this.seriesName.set((event.target as HTMLInputElement).value);
  }

  private resetForm(): void {
    this.seriesName.set('');
    this.errorMessage.set(null);
  }
}
