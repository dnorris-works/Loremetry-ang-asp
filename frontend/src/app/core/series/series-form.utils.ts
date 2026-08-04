import { SeriesBibleDocument, SeriesDetail } from './series.models';
import { StoryDocumentInput } from '../stories/story.models';

function toDocumentInput(document: SeriesBibleDocument): StoryDocumentInput {
  return {
    id: document.id,
    fileName: document.fileName,
    mimeType: document.mimeType,
    textContent: document.textContent ?? null,
    binaryContentBase64: document.binaryContentBase64 ?? null,
  };
}

export function populateSeriesForm(detail: SeriesDetail): {
  name: string;
  characters: StoryDocumentInput[];
  locations: StoryDocumentInput[];
} {
  const characters: StoryDocumentInput[] = [];
  const locations: StoryDocumentInput[] = [];

  for (const document of detail.bibleDocuments) {
    const input = toDocumentInput(document);

    if (document.category === 'character') {
      characters.push(input);
    } else {
      locations.push(input);
    }
  }

  return {
    name: detail.name,
    characters,
    locations,
  };
}
