import { TextDiffSegment } from './review.models';

const TOKEN_PATTERN = /(\s+|[^\s]+)/g;

function tokenize(text: string): string[] {
  const tokens = text.match(TOKEN_PATTERN);
  return tokens ?? [];
}

/**
 * Word-level diff for inline insert/delete display in review hunks.
 */
export function buildWordDiffSegments(before: string, after: string): TextDiffSegment[] {
  const left = tokenize(before);
  const right = tokenize(after);
  const rows = left.length;
  const cols = right.length;
  const lcs = Array.from({ length: rows + 1 }, () => Array<number>(cols + 1).fill(0));

  for (let i = rows - 1; i >= 0; i -= 1) {
    for (let j = cols - 1; j >= 0; j -= 1) {
      if (left[i] === right[j]) {
        lcs[i][j] = lcs[i + 1][j + 1] + 1;
      } else {
        lcs[i][j] = Math.max(lcs[i + 1][j], lcs[i][j + 1]);
      }
    }
  }

  const segments: TextDiffSegment[] = [];
  let i = 0;
  let j = 0;

  const pushSegment = (type: TextDiffSegment['type'], text: string): void => {
    if (!text) {
      return;
    }

    const last = segments.at(-1);
    if (last?.type === type) {
      last.text += text;
      return;
    }

    segments.push({ type, text });
  };

  while (i < rows && j < cols) {
    if (left[i] === right[j]) {
      pushSegment('equal', left[i]);
      i += 1;
      j += 1;
      continue;
    }

    if (lcs[i + 1][j] >= lcs[i][j + 1]) {
      pushSegment('delete', left[i]);
      i += 1;
    } else {
      pushSegment('insert', right[j]);
      j += 1;
    }
  }

  while (i < rows) {
    pushSegment('delete', left[i]);
    i += 1;
  }

  while (j < cols) {
    pushSegment('insert', right[j]);
    j += 1;
  }

  return segments;
}
