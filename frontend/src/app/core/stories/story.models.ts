export interface Story {
  id: string;
  name: string;
  manuscriptFiles: string[];
  bibleFiles: string[];
  createdAt: string;
}

export interface CreateStoryRequest {
  name: string;
  manuscriptFiles: string[];
  bibleFiles: string[];
}
