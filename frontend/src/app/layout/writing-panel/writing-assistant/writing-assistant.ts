import {
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import {
  AssistantChatMessage,
  AssistantContextEntitySummary,
} from '../../../core/writing/assistant-context.models';
import { findMentionedEntities, toEntitySummary } from '../../../core/writing/assistant-prompt-builder';
import { WritingAssistantApiService } from '../../../core/writing/writing-assistant-api.service';
import { WritingAssistantContextService } from '../../../core/writing/writing-assistant-context.service';
import { WritingService } from '../../../core/writing/writing.service';

@Component({
  selector: 'app-writing-assistant',
  templateUrl: './writing-assistant.html',
  styleUrl: './writing-assistant.css',
})
export class WritingAssistant {
  readonly documentTitle = input('');

  private readonly writingService = inject(WritingService);
  private readonly contextService = inject(WritingAssistantContextService);
  private readonly assistantApi = inject(WritingAssistantApiService);

  protected readonly prompt = signal('');
  protected readonly messages = signal<AssistantChatMessage[]>([]);
  protected readonly isSending = signal(false);
  protected readonly chatError = signal<string | null>(null);

  protected readonly catalog = this.contextService.catalog;
  protected readonly isCatalogLoading = this.contextService.isCatalogLoading;
  protected readonly catalogError = this.contextService.catalogError;

  protected readonly pendingMatches = computed(() => {
    const prompt = this.prompt().trim();
    if (!prompt) {
      return [];
    }

    return findMentionedEntities(prompt, this.catalog()).map(toEntitySummary);
  });

  constructor() {
    effect(() => {
      const destinationKey = this.writingService.destinationKey();
      const documentContext = this.writingService.documentContext();

      void this.contextService.refreshCatalog({
        destinationKey,
        documentContext,
      });
    });
  }

  protected async sendMessage(): Promise<void> {
    const userPrompt = this.prompt().trim();
    if (!userPrompt || this.isSending()) {
      return;
    }

    this.isSending.set(true);
    this.chatError.set(null);

    const context = this.contextService.buildPromptContext(userPrompt, {
      currentDocumentTitle: this.documentTitle(),
      currentDocumentContent: this.writingService.content(),
    });

    const userMessage: AssistantChatMessage = {
      id: createMessageId(),
      role: 'user',
      text: context.userPrompt,
      matchedEntities: context.matchedEntities.map(toEntitySummary),
    };

    this.messages.update((existing) => [...existing, userMessage]);
    this.prompt.set('');

    try {
      const response = await firstValueFrom(
        this.assistantApi.chat({
          userPrompt: context.userPrompt,
          augmentedPrompt: context.augmentedPrompt,
        }),
      );

      const assistantMessage: AssistantChatMessage = {
        id: createMessageId(),
        role: 'assistant',
        text: response.text,
        matchedEntities: context.matchedEntities.map(toEntitySummary),
      };

      this.messages.update((existing) => [...existing, assistantMessage]);
    } catch (error) {
      this.chatError.set(readChatErrorMessage(error));
    } finally {
      this.isSending.set(false);
    }
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    void this.sendMessage();
  }

  protected entityLabel(entity: AssistantContextEntitySummary): string {
    switch (entity.kind) {
      case 'manuscript':
        return `Chapter: ${entity.name}`;
      case 'character':
        return `Character: ${entity.name}`;
      case 'location':
        return `Location: ${entity.name}`;
      case 'story':
        return `Story: ${entity.name}`;
      case 'series':
        return `Series: ${entity.name}`;
      default:
        return entity.name;
    }
  }

  protected onPromptInput(event: Event): void {
    this.prompt.set((event.target as HTMLTextAreaElement).value);
  }
}

function createMessageId(): string {
  return `msg-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function readChatErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (typeof error.error?.detail === 'string' && error.error.detail.trim()) {
      return error.error.detail;
    }

    if (typeof error.error?.message === 'string' && error.error.message.trim()) {
      return error.error.message;
    }

    if (typeof error.error?.title === 'string' && error.status === 503) {
      return `${error.error.title}. Configure tokenmix_api_key in .env or Admin → Platform.`;
    }

    if (error.status === 0) {
      return 'Could not reach the server. Check that the backend is running.';
    }
  }

  return 'Assistant request failed.';
}
