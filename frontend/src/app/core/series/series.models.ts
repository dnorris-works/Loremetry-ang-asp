import { StoryDocumentInput } from '../stories/story.models';

export interface SeriesBibleDocument {
  id: number;
  category: 'character' | 'location';
  fileName: string;
  mimeType: string;
  textContent?: string | null;
  binaryContentBase64?: string | null;
  sortOrder: number;
}

export interface Series {
  id: number;
  name: string;
  characterCount: number;
  locationCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSeriesRequest {
  name: string;
  characters: StoryDocumentInput[];
  locations: StoryDocumentInput[];
}

export interface SeriesDetail {
  id: number;
  name: string;
  bibleDocuments: SeriesBibleDocument[];
  createdAt: string;
  updatedAt: string;
}

export interface SeriesPanelDraft {
  name: string;
  characters: StoryDocumentInput[];
  locations: StoryDocumentInput[];
}

export type UpdateSeriesRequest = CreateSeriesRequest;
