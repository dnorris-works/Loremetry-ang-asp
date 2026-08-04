import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { SeriesApiService } from '../series/series-api.service';
import { SeriesDetail } from '../series/series.models';
import { StoriesApiService } from '../stories/stories-api.service';
import { StoryDetail } from '../stories/story.models';
import { AssistantContextEntity } from './assistant-context.models';
import {
  buildPromptContext,
  seriesDocumentEntity,
  storyDocumentEntity,
} from './assistant-prompt-builder';
import { deriveDocumentSearchTerms } from './assistant-entity-name.utils';
import { WritingDocumentContext } from './writing.models';

@Injectable({ providedIn: 'root' })
export class WritingAssistantContextService {
  private readonly storiesApi = inject(StoriesApiService);
  private readonly seriesApi = inject(SeriesApiService);

  readonly catalog = signal<ReadonlyArray<AssistantContextEntity>>([]);
  readonly isCatalogLoading = signal(false);
  readonly catalogError = signal<string | null>(null);

  async refreshCatalog(params: {
    destinationKey: string;
    documentContext: WritingDocumentContext | null;
  }): Promise<void> {
    this.isCatalogLoading.set(true);
    this.catalogError.set(null);

    try {
      const catalog = await this.loadCatalog(params);
      this.catalog.set(catalog);
    } catch {
      this.catalog.set([]);
      this.catalogError.set('Could not load lore references for context matching.');
    } finally {
      this.isCatalogLoading.set(false);
    }
  }

  buildPromptContext(
    userPrompt: string,
    options?: {
      currentDocumentTitle?: string;
      currentDocumentContent?: string;
    },
  ) {
    return buildPromptContext(userPrompt, this.catalog(), options);
  }

  private async loadCatalog(params: {
    destinationKey: string;
    documentContext: WritingDocumentContext | null;
  }): Promise<AssistantContextEntity[]> {
    if (params.documentContext) {
      return await this.loadCatalogForDocumentContext(params.documentContext);
    }

    return await this.loadCatalogForDestinationKey(params.destinationKey);
  }

  private async loadCatalogForDocumentContext(
    context: WritingDocumentContext,
  ): Promise<AssistantContextEntity[]> {
    if (context.source === 'story') {
      const story = await firstValueFrom(this.storiesApi.getStory(context.parentId));
      const entities = this.entitiesFromStory(story);

      const summaries = await firstValueFrom(this.storiesApi.listStories());
      const summary = summaries.find((item) => item.id === story.id);
      if (summary?.seriesId) {
        const series = await firstValueFrom(this.seriesApi.getSeries(summary.seriesId));
        entities.push(...this.entitiesFromSeries(series));
      }

      return entities;
    }

    const series = await firstValueFrom(this.seriesApi.getSeries(context.parentId));
    return this.entitiesFromSeries(series);
  }

  private async loadCatalogForDestinationKey(destinationKey: string): Promise<AssistantContextEntity[]> {
    const storyMatch = destinationKey.match(/^story:(\d+)$/);
    if (storyMatch) {
      const storyId = Number(storyMatch[1]);
      const story = await firstValueFrom(this.storiesApi.getStory(storyId));
      const entities = this.entitiesFromStory(story);

      const summaries = await firstValueFrom(this.storiesApi.listStories());
      const summary = summaries.find((item) => item.id === storyId);
      if (summary?.seriesId) {
        const series = await firstValueFrom(this.seriesApi.getSeries(summary.seriesId));
        entities.push(...this.entitiesFromSeries(series));
      }

      return entities;
    }

    const seriesMatch = destinationKey.match(/^series:(\d+)$/);
    if (seriesMatch) {
      const series = await firstValueFrom(this.seriesApi.getSeries(Number(seriesMatch[1])));
      return this.entitiesFromSeries(series);
    }

    return [];
  }

  private entitiesFromStory(story: StoryDetail): AssistantContextEntity[] {
    const entities: AssistantContextEntity[] = [
      {
        kind: 'story',
        name: story.name,
        searchTerms: deriveDocumentSearchTerms(story.name),
        source: 'story',
        parentId: story.id,
        parentName: story.name,
        fileName: story.name,
        textContent: null,
      },
    ];

    for (const document of story.documents) {
      entities.push(
        storyDocumentEntity(
          document.kind,
          document.fileName,
          document.textContent,
          story.id,
          story.name,
          document.id,
        ),
      );
    }

    return entities;
  }

  private entitiesFromSeries(series: SeriesDetail): AssistantContextEntity[] {
    const entities: AssistantContextEntity[] = [
      {
        kind: 'series',
        name: series.name,
        searchTerms: deriveDocumentSearchTerms(series.name),
        source: 'series',
        parentId: series.id,
        parentName: series.name,
        fileName: series.name,
        textContent: null,
      },
    ];

    for (const document of series.bibleDocuments) {
      entities.push(
        seriesDocumentEntity(
          document.category,
          document.fileName,
          document.textContent,
          series.id,
          series.name,
          document.id,
        ),
      );
    }

    return entities;
  }
}
