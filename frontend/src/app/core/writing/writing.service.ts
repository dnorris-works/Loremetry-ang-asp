import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { DocumentTypesApiService } from '../document-types/document-types-api.service';
import { DocumentType } from '../document-types/document-types.models';
import { SeriesService } from '../series/series.service';
import { StoriesService } from '../stories/stories.service';
import { WritingDraftApiService } from './writing-draft-api.service';
import {
  PANEL_DRAFT_KEY,
  UpsertWritingDraftRequest,
  buildDocumentDraftKey,
} from './writing-draft.models';
import {
  parseWritingDestinationKey,
  writingDestinationId,
  writingDestinationKey,
  WritingDestination,
  WritingDocumentContext,
} from './writing.models';

@Injectable({ providedIn: 'root' })
export class WritingService {
  private readonly storiesService = inject(StoriesService);
  private readonly seriesService = inject(SeriesService);
  private readonly documentTypesApi = inject(DocumentTypesApiService);
  private readonly draftApi = inject(WritingDraftApiService);

  private contentFlusher: (() => string) | null = null;

  readonly isPanelOpen = signal(false);
  readonly title = signal('');
  readonly content = signal('');
  readonly savedContent = signal('');
  readonly savedTitle = signal('');
  readonly documentContext = signal<WritingDocumentContext | null>(null);
  readonly documentTypes = signal<ReadonlyArray<DocumentType>>([]);
  readonly documentTypeCode = signal('manuscript');
  readonly destinationKey = signal('');
  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly destinationSeries = computed(() => this.seriesService.series());
  readonly destinationStories = computed(() => this.storiesService.stories());
  readonly hasDestinations = computed(
    () => this.destinationSeries().length > 0 || this.destinationStories().length > 0,
  );
  readonly selectedDestination = computed((): WritingDestination | null => {
    const key = this.destinationKey();
    const source = parseWritingDestinationKey(key);
    const id = writingDestinationId(key);

    if (!source || id === null) {
      return null;
    }

    if (source === 'story') {
      const story = this.destinationStories().find((item) => item.id === id);
      return story ? { source, id, name: story.name } : null;
    }

    const series = this.destinationSeries().find((item) => item.id === id);
    return series ? { source, id, name: series.name } : null;
  });

  readonly isDirty = computed(() => {
    if (this.isDocumentMode()) {
      return this.content() !== this.savedContent();
    }

    return (
      this.content() !== this.savedContent() ||
      this.title() !== this.savedTitle() ||
      this.documentTypeCode() !== this.savedDocumentTypeCode() ||
      this.destinationKey() !== this.savedDestinationKey()
    );
  });
  readonly isDocumentMode = computed(() => this.documentContext() !== null);
  readonly documentTypeDisplayName = computed(() => {
    const code = this.documentTypeCode();
    const type = this.documentTypes().find((item) => item.code === code);
    return type?.displayName ?? code;
  });

  private readonly savedDocumentTypeCode = signal('manuscript');
  private readonly savedDestinationKey = signal('');

  registerContentFlusher(flusher: () => string): void {
    this.contentFlusher = flusher;
  }

  openPanel(): void {
    void this.openPanelAsync();
  }

  private async openPanelAsync(): Promise<void> {
    if (this.isPanelOpen()) {
      const saved = await this.persistDraftIfDirty();
      if (!saved) {
        return;
      }
    }

    this.documentContext.set(null);
    this.title.set('');
    this.content.set('');
    this.savedContent.set('');
    this.savedTitle.set('');
    this.documentTypeCode.set('manuscript');
    this.savedDocumentTypeCode.set('manuscript');
    this.destinationKey.set('');
    this.savedDestinationKey.set('');
    this.saveError.set(null);
    this.isPanelOpen.set(true);
    await this.initializeDraftPanel();
  }

  openDocument(params: {
    fileName: string;
    content: string;
    mimeType: string;
    context: WritingDocumentContext;
  }): void {
    void this.openDocumentAsync(params);
  }

  private async openDocumentAsync(params: {
    fileName: string;
    content: string;
    mimeType: string;
    context: WritingDocumentContext;
  }): Promise<void> {
    if (this.isPanelOpen()) {
      const saved = await this.persistDraftIfDirty();
      if (!saved) {
        return;
      }
    }

    this.documentContext.set(params.context);
    this.title.set(params.fileName);
    this.content.set(params.content);
    this.savedContent.set(params.content);
    this.savedTitle.set(params.fileName);
    this.documentTypeCode.set(params.context.category);
    this.savedDocumentTypeCode.set(params.context.category);
    this.destinationKey.set(
      writingDestinationKey(params.context.source, params.context.parentId),
    );
    this.savedDestinationKey.set(this.destinationKey());
    this.saveError.set(null);
    this.isPanelOpen.set(true);
    await this.loadDocumentTypes(params.context.source);
    await this.restoreDocumentDraft(params.context, params.content);
  }

  closePanel(): void {
    void this.closePanelAsync();
  }

  async closePanelAsync(): Promise<void> {
    if (this.isPanelOpen()) {
      const saved = await this.persistDraftIfDirty();
      if (!saved) {
        return;
      }
    }

    this.clearPanelState();
  }

  setDestinationKey(key: string): void {
    this.destinationKey.set(key);
    const source = parseWritingDestinationKey(key);
    if (source) {
      void this.loadDocumentTypes(source);
    }
  }

  async save(): Promise<boolean> {
    const context = this.documentContext();
    const textContent = this.getCurrentContent();
    this.content.set(textContent);

    if (!context) {
      return false;
    }

    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      if (context.parentId > 0) {
        let documentId = context.documentId ?? (await this.lookupDocumentId(context));

        if (!documentId) {
          documentId = await this.lookupDocumentId(context, true);
        }

        if (documentId) {
          const resolvedId = await this.persistWithRetry(context, documentId, textContent);
          if (!resolvedId) {
            return false;
          }

          this.documentContext.set({ ...context, documentId: resolvedId });
          this.savedContent.set(textContent);
          await this.deleteCurrentDraft();
          return true;
        }

        this.saveError.set(
          `Could not find "${context.fileName}" in the ${context.source}. Save the ${context.source} first or reopen the file.`,
        );
        return false;
      }

      if (context.source === 'story') {
        this.storiesService.updatePanelDocumentText(
          context.fileName,
          context.category,
          textContent,
        );
      } else {
        this.seriesService.updatePanelDocumentText(
          context.fileName,
          context.category,
          textContent,
        );
      }

      this.savedContent.set(textContent);
      await this.deleteCurrentDraft();
      return true;
    } finally {
      this.isSaving.set(false);
    }
  }

  reset(): void {
    void this.closePanelAsync();
  }

  private clearPanelState(): void {
    this.isPanelOpen.set(false);
    this.documentContext.set(null);
    this.title.set('');
    this.content.set('');
    this.savedContent.set('');
    this.savedTitle.set('');
    this.documentTypeCode.set('manuscript');
    this.savedDocumentTypeCode.set('manuscript');
    this.destinationKey.set('');
    this.savedDestinationKey.set('');
    this.saveError.set(null);
  }

  private getCurrentContent(): string {
    return this.contentFlusher?.() ?? this.content();
  }

  private currentDraftKey(): string {
    const context = this.documentContext();
    if (context) {
      return buildDocumentDraftKey(context);
    }

    return PANEL_DRAFT_KEY;
  }

  private buildDraftRequest(textContent: string): UpsertWritingDraftRequest {
    const context = this.documentContext();

    if (context) {
      return {
        draftKey: buildDocumentDraftKey(context),
        mode: 'document',
        source: context.source,
        parentId: context.parentId,
        documentId: context.documentId ?? null,
        category: context.category,
        title: this.title(),
        fileName: context.fileName,
        textContent,
        destinationKey: this.destinationKey(),
        mimeType: context.mimeType,
      };
    }

    return {
      draftKey: PANEL_DRAFT_KEY,
      mode: 'draft',
      source: parseWritingDestinationKey(this.destinationKey()),
      parentId: writingDestinationId(this.destinationKey()),
      documentId: null,
      category: this.documentTypeCode(),
      title: this.title(),
      fileName: '',
      textContent,
      destinationKey: this.destinationKey(),
      mimeType: 'text/markdown',
    };
  }

  private async persistDraftIfDirty(): Promise<boolean> {
    const textContent = this.getCurrentContent();
    this.content.set(textContent);

    if (!this.isDirty()) {
      return true;
    }

    try {
      await firstValueFrom(this.draftApi.upsertDraft(this.buildDraftRequest(textContent)));
      return true;
    } catch {
      this.saveError.set('Could not save your draft before leaving the Write panel.');
      return false;
    }
  }

  private async deleteCurrentDraft(): Promise<void> {
    try {
      await firstValueFrom(this.draftApi.deleteDraft(this.currentDraftKey()));
    } catch {
      // Draft cleanup is best-effort after an explicit save.
    }
  }

  private async restorePanelDraft(): Promise<void> {
    try {
      const draft = await firstValueFrom(this.draftApi.getDraft(PANEL_DRAFT_KEY));
      if (!draft) {
        return;
      }

      this.title.set(draft.title);
      this.content.set(draft.textContent);
      this.savedContent.set(draft.textContent);
      this.savedTitle.set(draft.title);
      this.documentTypeCode.set(draft.category || 'manuscript');
      this.savedDocumentTypeCode.set(draft.category || 'manuscript');
      this.destinationKey.set(draft.destinationKey);
      this.savedDestinationKey.set(draft.destinationKey);

      const source = parseWritingDestinationKey(draft.destinationKey);
      if (source) {
        await this.loadDocumentTypes(source);
      }
    } catch {
      // No saved panel draft.
    }
  }

  private async restoreDocumentDraft(
    context: WritingDocumentContext,
    baselineContent: string,
  ): Promise<void> {
    try {
      const draft = await firstValueFrom(this.draftApi.getDraft(buildDocumentDraftKey(context)));
      if (!draft || draft.textContent === baselineContent) {
        return;
      }

      this.content.set(draft.textContent);
      this.savedContent.set(baselineContent);
    } catch {
      // No saved document draft.
    }
  }

  private async initializeDraftPanel(): Promise<void> {
    const [storiesLoaded, seriesLoaded] = await Promise.all([
      this.storiesService.refreshStories(),
      this.seriesService.refreshSeries(),
    ]);

    if (!storiesLoaded || !seriesLoaded) {
      this.saveError.set('Could not load stories and series.');
    }

    this.applyDefaultDestination();
    await this.restorePanelDraft();
  }

  private applyDefaultDestination(): void {
    const stories = this.destinationStories();
    const series = this.destinationSeries();
    const currentKey = this.destinationKey();

    if (this.isValidDestinationKey(currentKey, stories, series)) {
      const source = parseWritingDestinationKey(currentKey);
      if (source) {
        void this.loadDocumentTypes(source);
      }
      return;
    }

    if (stories.length > 0) {
      this.setDestinationKey(writingDestinationKey('story', stories[0].id));
      return;
    }

    if (series.length > 0) {
      this.setDestinationKey(writingDestinationKey('series', series[0].id));
      return;
    }

    this.destinationKey.set('');
    void this.loadDocumentTypes('story');
  }

  private isValidDestinationKey(
    key: string,
    stories: ReadonlyArray<{ id: number }>,
    series: ReadonlyArray<{ id: number }>,
  ): boolean {
    const source = parseWritingDestinationKey(key);
    const id = writingDestinationId(key);

    if (!source || id === null) {
      return false;
    }

    if (source === 'story') {
      return stories.some((item) => item.id === id);
    }

    return series.some((item) => item.id === id);
  }

  private async loadDocumentTypes(parent: 'story' | 'series'): Promise<void> {
    try {
      const types = await firstValueFrom(this.documentTypesApi.listDocumentTypes(parent));
      this.documentTypes.set(types);

      if (!types.some((type) => type.code === this.documentTypeCode())) {
        const fallback = types[0]?.code ?? 'manuscript';
        this.documentTypeCode.set(fallback);
      }
    } catch {
      this.documentTypes.set([]);
      this.saveError.set('Could not load document types.');
    }
  }

  private async persistWithRetry(
    context: WritingDocumentContext,
    documentId: number,
    textContent: string,
  ): Promise<number | null> {
    if (await this.persistDocumentText(context, documentId, textContent)) {
      return documentId;
    }

    const refreshedId = await this.lookupDocumentId(context, true);
    if (!refreshedId || refreshedId === documentId) {
      return null;
    }

    return (await this.persistDocumentText(context, refreshedId, textContent)) ? refreshedId : null;
  }

  private async persistDocumentText(
    context: WritingDocumentContext,
    documentId: number,
    textContent: string,
  ): Promise<boolean> {
    const saved =
      context.source === 'story'
        ? await this.storiesService.updateStoryDocumentText(
            context.parentId,
            documentId,
            textContent,
          )
        : await this.seriesService.updateSeriesDocumentText(
            context.parentId,
            documentId,
            textContent,
          );

    if (saved) {
      return true;
    }

    const serviceError =
      context.source === 'story'
        ? this.storiesService.saveError()
        : this.seriesService.saveError();

    this.saveError.set(serviceError ?? 'Failed to save document.');
    return false;
  }

  private async lookupDocumentId(
    context: WritingDocumentContext,
    forceRefresh = false,
  ): Promise<number | null> {
    if (context.source === 'story') {
      const detail = await this.storiesService.ensureEditingDetail(context.parentId, forceRefresh);

      const document = detail?.documents.find(
        (item) => item.fileName === context.fileName && item.kind === context.category,
      );
      return document?.id ?? null;
    }

    const detail = await this.seriesService.ensureEditingDetail(context.parentId, forceRefresh);

    const document = detail?.bibleDocuments.find(
      (item) => item.fileName === context.fileName && item.category === context.category,
    );
    return document?.id ?? null;
  }
}
