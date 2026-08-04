export type MarkdownFormatAction =
  | 'bold'
  | 'italic'
  | 'strikethrough'
  | 'heading1'
  | 'heading2'
  | 'heading3'
  | 'bulletList'
  | 'orderedList'
  | 'blockquote'
  | 'codeBlock'
  | 'link'
  | 'horizontalRule';

interface TextEditResult {
  value: string;
  selectionStart: number;
  selectionEnd: number;
}

export function applyMarkdownFormat(
  value: string,
  selectionStart: number,
  selectionEnd: number,
  action: MarkdownFormatAction,
): TextEditResult {
  switch (action) {
    case 'bold':
      return wrapSelection(value, selectionStart, selectionEnd, '**', '**');
    case 'italic':
      return wrapSelection(value, selectionStart, selectionEnd, '_', '_');
    case 'strikethrough':
      return wrapSelection(value, selectionStart, selectionEnd, '~~', '~~');
    case 'heading1':
      return prefixLine(value, selectionStart, selectionEnd, '# ');
    case 'heading2':
      return prefixLine(value, selectionStart, selectionEnd, '## ');
    case 'heading3':
      return prefixLine(value, selectionStart, selectionEnd, '### ');
    case 'bulletList':
      return prefixLine(value, selectionStart, selectionEnd, '- ');
    case 'orderedList':
      return prefixLine(value, selectionStart, selectionEnd, '1. ');
    case 'blockquote':
      return prefixLine(value, selectionStart, selectionEnd, '> ');
    case 'codeBlock':
      return wrapSelection(value, selectionStart, selectionEnd, '```\n', '\n```');
    case 'link':
      return wrapLink(value, selectionStart, selectionEnd);
    case 'horizontalRule':
      return insertText(value, selectionStart, selectionEnd, '\n---\n');
  }
}

function wrapSelection(
  value: string,
  selectionStart: number,
  selectionEnd: number,
  before: string,
  after: string,
): TextEditResult {
  const selected = value.slice(selectionStart, selectionEnd);
  const replacement = `${before}${selected}${after}`;
  const nextValue =
    value.slice(0, selectionStart) + replacement + value.slice(selectionEnd);

  return {
    value: nextValue,
    selectionStart: selectionStart + before.length,
    selectionEnd: selectionStart + before.length + selected.length,
  };
}

function wrapLink(
  value: string,
  selectionStart: number,
  selectionEnd: number,
): TextEditResult {
  const selected = value.slice(selectionStart, selectionEnd);
  const label = selected || 'link text';
  const replacement = `[${label}](url)`;
  const nextValue =
    value.slice(0, selectionStart) + replacement + value.slice(selectionEnd);

  const urlStart = selectionStart + label.length + 3;
  const urlEnd = urlStart + 3;

  return {
    value: nextValue,
    selectionStart: urlStart,
    selectionEnd: urlEnd,
  };
}

function prefixLine(
  value: string,
  selectionStart: number,
  selectionEnd: number,
  prefix: string,
): TextEditResult {
  const lineStart = value.lastIndexOf('\n', selectionStart - 1) + 1;
  const lineEnd = value.indexOf('\n', selectionEnd);
  const end = lineEnd === -1 ? value.length : lineEnd;
  const line = value.slice(lineStart, end);
  const stripped = line.replace(/^#{1,6}\s+|^[-*+]\s+|^\d+\.\s+|^>\s+/, '');
  const prefixed = `${prefix}${stripped}`;
  const nextValue = value.slice(0, lineStart) + prefixed + value.slice(end);

  return {
    value: nextValue,
    selectionStart: lineStart,
    selectionEnd: lineStart + prefixed.length,
  };
}

function insertText(
  value: string,
  selectionStart: number,
  selectionEnd: number,
  text: string,
): TextEditResult {
  const nextValue = value.slice(0, selectionStart) + text + value.slice(selectionEnd);

  return {
    value: nextValue,
    selectionStart: selectionStart + text.length,
    selectionEnd: selectionStart + text.length,
  };
}
