import { StoryDetail, StoryDocument, StoryDocumentInput } from './story.models';

export function toStoryDocumentInput(document: StoryDocument): StoryDocumentInput {
  return {
    fileName: document.fileName,
    mimeType: document.mimeType,
    textContent: document.textContent ?? null,
    binaryContentBase64: document.binaryContentBase64 ?? null,
  };
}

export function splitStoryDocuments(documents: StoryDocument[]): {
  manuscripts: StoryDocumentInput[];
  characters: StoryDocumentInput[];
  locations: StoryDocumentInput[];
} {
  const manuscripts: StoryDocumentInput[] = [];
  const characters: StoryDocumentInput[] = [];
  const locations: StoryDocumentInput[] = [];

  for (const document of documents) {
    const input = toStoryDocumentInput(document);

    switch (document.kind) {
      case 'manuscript':
        manuscripts.push(input);
        break;
      case 'character':
        characters.push(input);
        break;
      case 'location':
        locations.push(input);
        break;
    }
  }

  return { manuscripts, characters, locations };
}

export function populateStoryForm(detail: StoryDetail): {
  name: string;
  manuscripts: StoryDocumentInput[];
  characters: StoryDocumentInput[];
  locations: StoryDocumentInput[];
} {
  const { manuscripts, characters, locations } = splitStoryDocuments(detail.documents);

  return {
    name: detail.name,
    manuscripts,
    characters,
    locations,
  };
}
