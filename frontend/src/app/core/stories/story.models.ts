export interface StoryDocumentInput {
  fileName: string;
  mimeType: string;
  textContent?: string | null;
  binaryContentBase64?: string | null;
}

export interface StoryDocument {
  id: number;
  kind: 'manuscript' | 'character' | 'location';
  fileName: string;
  mimeType: string;
  textContent?: string | null;
  binaryContentBase64?: string | null;
  sortOrder: number;
}

export interface Story {
  id: number;
  name: string;
  manuscriptCount: number;
  characterCount: number;
  locationCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateStoryRequest {
  name: string;
  manuscripts: StoryDocumentInput[];
  characters: StoryDocumentInput[];
  locations: StoryDocumentInput[];
}

export interface StoryDetail {
  id: number;
  name: string;
  documents: StoryDocument[];
  createdAt: string;
  updatedAt: string;
}

export type UpdateStoryRequest = CreateStoryRequest;
