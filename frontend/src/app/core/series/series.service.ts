import { Injectable, signal } from '@angular/core';

import { CreateSeriesRequest, Series } from './series.models';

@Injectable({ providedIn: 'root' })
export class SeriesService {
  readonly series = signal<Series[]>([]);
  readonly isAddPanelOpen = signal(false);

  openAddPanel(): void {
    this.isAddPanelOpen.set(true);
  }

  closeAddPanel(): void {
    this.isAddPanelOpen.set(false);
  }

  addSeries(request: CreateSeriesRequest): Series {
    const item: Series = {
      id: crypto.randomUUID(),
      name: request.name.trim(),
      createdAt: new Date().toISOString(),
    };

    this.series.update((series) => [item, ...series]);
    this.closeAddPanel();

    return item;
  }
}
