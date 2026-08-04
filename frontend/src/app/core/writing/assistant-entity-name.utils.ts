export function deriveDocumentDisplayName(fileName: string): string {
  const normalizedPath = fileName.replace(/\\/g, '/');
  const baseName = normalizedPath.split('/').pop() ?? fileName;
  const withoutExtension = baseName.replace(/\.(md|markdown|txt|docx)$/i, '');
  return withoutExtension.replace(/[-_]+/g, ' ').trim();
}

export function deriveDocumentSearchTerms(fileName: string, parentName?: string): string[] {
  const terms = new Set<string>();
  const displayName = deriveDocumentDisplayName(fileName);

  if (displayName.length >= 2) {
    terms.add(displayName);
  }

  const pathSegments = fileName
    .replace(/\\/g, '/')
    .split('/')
    .map((segment) => segment.replace(/\.(md|markdown|txt|docx)$/i, '').replace(/[-_]+/g, ' ').trim())
    .filter((segment) => segment.length >= 2);

  for (const segment of pathSegments) {
    terms.add(segment);
  }

  if (parentName && parentName.trim().length >= 2) {
    terms.add(parentName.trim());
  }

  return [...terms];
}

export function promptMentionsTerm(prompt: string, term: string): boolean {
  const trimmedTerm = term.trim();
  if (trimmedTerm.length < 2) {
    return false;
  }

  const escaped = trimmedTerm.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

  if (trimmedTerm.includes(' ')) {
    return new RegExp(escaped, 'i').test(prompt);
  }

  return new RegExp(`\\b${escaped}\\b`, 'i').test(prompt);
}
