export interface WritingDraft {
  draftKey: string;
  mode: 'draft' | 'document';
  source: 'story' | 'series' | null;
  parentId: number | null;
  documentId: number | null;
  category: string;
  title: string;
  fileName: string;
  textContent: string;
  destinationKey: string;
  mimeType: string;
  updatedAt: string;
}

export interface UpsertWritingDraftRequest {
  draftKey: string;
  mode: 'draft' | 'document';
  source: 'story' | 'series' | null;
  parentId: number | null;
  documentId: number | null;
  category: string;
  title: string;
  fileName: string;
  textContent: string;
  destinationKey: string;
  mimeType: string;
}

export const PANEL_DRAFT_KEY = 'panel:draft';

export function buildDocumentDraftKey(context: {
  source: 'story' | 'series';
  parentId: number;
  category: string;
  fileName: string;
}): string {
  return `doc:${context.source}:${context.parentId}:${context.category}:${context.fileName}`;
}
