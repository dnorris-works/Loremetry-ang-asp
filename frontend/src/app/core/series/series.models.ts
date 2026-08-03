import { StoryDocumentInput } from '../stories/story.models';

export interface Series {
  id: number;
  name: string;
  bibleCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSeriesRequest {
  name: string;
  bibles: StoryDocumentInput[];
}
