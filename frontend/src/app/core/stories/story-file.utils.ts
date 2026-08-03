import { StoryDocumentInput } from './story.models';

const DOCX_MIME =
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document';

export async function readStoryFile(file: File): Promise<StoryDocumentInput> {
  const fileName = file.name;
  const lowerName = fileName.toLowerCase();

  if (lowerName.endsWith('.md')) {
    return {
      fileName,
      mimeType: file.type || 'text/markdown',
      textContent: await file.text(),
    };
  }

  if (lowerName.endsWith('.txt')) {
    return {
      fileName,
      mimeType: file.type || 'text/plain',
      textContent: await file.text(),
    };
  }

  if (lowerName.endsWith('.docx')) {
    const buffer = await file.arrayBuffer();
    return {
      fileName,
      mimeType: file.type || DOCX_MIME,
      binaryContentBase64: arrayBufferToBase64(buffer),
    };
  }

  throw new Error(`Unsupported file type: ${fileName}`);
}

function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';

  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }

  return btoa(binary);
}

export function storyDocumentKey(document: StoryDocumentInput): string {
  return document.fileName.toLowerCase();
}
