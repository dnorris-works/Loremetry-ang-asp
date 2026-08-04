export type AssistantContextEntityKind =
  | 'character'
  | 'location'
  | 'manuscript'
  | 'story'
  | 'series';

export interface AssistantContextEntity {
  kind: AssistantContextEntityKind;
  name: string;
  searchTerms: string[];
  source: 'story' | 'series';
  parentId: number;
  parentName: string;
  documentId?: number;
  fileName: string;
  textContent?: string | null;
}

export interface AssistantContextEntitySummary {
  kind: AssistantContextEntityKind;
  name: string;
  parentName: string;
  fileName: string;
}

export interface AssistantPromptContext {
  userPrompt: string;
  matchedEntities: AssistantContextEntity[];
  augmentedPrompt: string;
}

export interface AssistantChatMessage {
  id: string;
  role: 'user' | 'assistant';
  text: string;
  matchedEntities: AssistantContextEntitySummary[];
}
