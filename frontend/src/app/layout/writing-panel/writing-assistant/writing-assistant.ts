import {
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';

import {
  AssistantChatMessage,
  AssistantContextEntitySummary,
} from '../../../core/writing/assistant-context.models';
import { findMentionedEntities, toEntitySummary } from '../../../core/writing/assistant-prompt-builder';
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

  protected readonly prompt = signal('');
  protected readonly messages = signal<AssistantChatMessage[]>([]);
  protected readonly isSending = signal(false);

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

    try {
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

      const assistantMessage: AssistantChatMessage = {
        id: createMessageId(),
        role: 'assistant',
        text: buildPlaceholderAssistantReply(context),
        matchedEntities: context.matchedEntities.map(toEntitySummary),
      };

      this.messages.update((existing) => [...existing, userMessage, assistantMessage]);
      this.prompt.set('');
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

function buildPlaceholderAssistantReply(
  context: ReturnType<WritingAssistantContextService['buildPromptContext']>,
): string {
  const matchCount = context.matchedEntities.length;
  const loreLabel =
    matchCount === 0
      ? 'No lore references were detected in your message.'
      : matchCount === 1
        ? '1 lore reference was included in the prepared prompt.'
        : `${matchCount} lore references were included in the prepared prompt.`;

  return `AI is not connected yet. ${loreLabel} When enabled, the assistant will use the open document plus matched characters, locations, and other documents to edit your draft.`;
}
