export interface StoryDocumentInput {
  fileName: string;
  mimeType: string;
  textContent?: string | null;
  binaryContentBase64?: string | null;
}

export interface Story {
  id: number;
  name: string;
  manuscriptCount: number;
  bibleCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateStoryRequest {
  name: string;
  manuscripts: StoryDocumentInput[];
  bibles: StoryDocumentInput[];
}
