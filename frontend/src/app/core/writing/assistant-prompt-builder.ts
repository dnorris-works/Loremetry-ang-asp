import {
  AssistantContextEntity,
  AssistantContextEntityKind,
  AssistantContextEntitySummary,
  AssistantPromptContext,
} from './assistant-context.models';
import { deriveDocumentDisplayName, deriveDocumentSearchTerms, promptMentionsTerm } from './assistant-entity-name.utils';

const MAX_CONTEXT_BODY_CHARS = 4000;

export function toEntitySummary(entity: AssistantContextEntity): AssistantContextEntitySummary {
  return {
    kind: entity.kind,
    name: entity.name,
    parentName: entity.parentName,
    fileName: entity.fileName,
  };
}

export function findMentionedEntities(
  prompt: string,
  catalog: ReadonlyArray<AssistantContextEntity>,
): AssistantContextEntity[] {
  const matches = new Map<string, AssistantContextEntity>();

  const rankedCatalog = [...catalog].sort((left, right) => {
    const leftMax = Math.max(...left.searchTerms.map((term) => term.length));
    const rightMax = Math.max(...right.searchTerms.map((term) => term.length));
    return rightMax - leftMax;
  });

  for (const entity of rankedCatalog) {
    const matched = entity.searchTerms.some((term) => promptMentionsTerm(prompt, term));
    if (matched) {
      matches.set(entityKey(entity), entity);
    }
  }

  return [...matches.values()];
}

export function buildAugmentedPrompt(params: {
  userPrompt: string;
  currentDocumentTitle?: string;
  currentDocumentContent?: string;
  matchedEntities: ReadonlyArray<AssistantContextEntity>;
}): string {
  const sections: string[] = [];

  if (params.currentDocumentTitle) {
    sections.push(
      formatContextSection(
        'Current document',
        params.currentDocumentTitle,
        params.currentDocumentContent,
      ),
    );
  }

  for (const entity of params.matchedEntities) {
    const label = `${formatKindLabel(entity.kind)}: ${entity.name} (${entity.parentName})`;
    sections.push(formatContextSection(label, entity.fileName, entity.textContent));
  }

  if (sections.length === 0) {
    return params.userPrompt;
  }

  return [
    '## Lore context (auto-included from your prompt)',
    ...sections,
    '',
    '## User request',
    params.userPrompt,
  ].join('\n');
}

export function buildPromptContext(
  userPrompt: string,
  catalog: ReadonlyArray<AssistantContextEntity>,
  options?: {
    currentDocumentTitle?: string;
    currentDocumentContent?: string;
  },
): AssistantPromptContext {
  const trimmedPrompt = userPrompt.trim();
  const matchedEntities = findMentionedEntities(trimmedPrompt, catalog);

  return {
    userPrompt: trimmedPrompt,
    matchedEntities,
    augmentedPrompt: buildAugmentedPrompt({
      userPrompt: trimmedPrompt,
      currentDocumentTitle: options?.currentDocumentTitle,
      currentDocumentContent: options?.currentDocumentContent,
      matchedEntities,
    }),
  };
}

function entityKey(entity: AssistantContextEntity): string {
  return `${entity.source}:${entity.parentId}:${entity.kind}:${entity.fileName}`;
}

function formatKindLabel(kind: AssistantContextEntityKind): string {
  switch (kind) {
    case 'manuscript':
      return 'Chapter';
    case 'character':
      return 'Character';
    case 'location':
      return 'Location';
    case 'story':
      return 'Story';
    case 'series':
      return 'Series';
  }
}

function formatContextSection(label: string, fileName: string, textContent?: string | null): string {
  const body = formatContextBody(textContent);
  return `### ${label}\nFile: ${fileName}\n${body}`;
}

function formatContextBody(textContent?: string | null): string {
  if (!textContent?.trim()) {
    return '(No text content available for this reference.)';
  }

  const trimmed = textContent.trim();
  if (trimmed.length <= MAX_CONTEXT_BODY_CHARS) {
    return trimmed;
  }

  return `${trimmed.slice(0, MAX_CONTEXT_BODY_CHARS)}\n…`;
}

export function storyDocumentEntity(
  kind: 'manuscript' | 'character' | 'location',
  fileName: string,
  textContent: string | null | undefined,
  storyId: number,
  storyName: string,
  documentId?: number,
): AssistantContextEntity {
  const entityKind: AssistantContextEntityKind =
    kind === 'manuscript' ? 'manuscript' : kind;

  return {
    kind: entityKind,
    name: deriveDocumentDisplayName(fileName),
    searchTerms: deriveDocumentSearchTerms(fileName, storyName),
    source: 'story',
    parentId: storyId,
    parentName: storyName,
    documentId,
    fileName,
    textContent,
  };
}

export function seriesDocumentEntity(
  category: 'character' | 'location',
  fileName: string,
  textContent: string | null | undefined,
  seriesId: number,
  seriesName: string,
  documentId?: number,
): AssistantContextEntity {
  return {
    kind: category,
    name: deriveDocumentDisplayName(fileName),
    searchTerms: deriveDocumentSearchTerms(fileName, seriesName),
    source: 'series',
    parentId: seriesId,
    parentName: seriesName,
    documentId,
    fileName,
    textContent,
  };
}
