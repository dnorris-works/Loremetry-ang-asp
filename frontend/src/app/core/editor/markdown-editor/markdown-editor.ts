import {
  Component,
  ElementRef,
  effect,
  model,
  signal,
  viewChild,
} from '@angular/core';

import { htmlToMarkdown, markdownToHtml } from '../markdown-converter';
import {
  applyMarkdownFormat,
  MarkdownFormatAction,
} from '../markdown-editor.utils';

type EditorViewMode = 'wysiwyg' | 'code';

interface ToolbarItem {
  action: MarkdownFormatAction;
  label: string;
  icon: string;
}

interface ToolbarGroup {
  items: ToolbarItem[];
}

@Component({
  selector: 'app-markdown-editor',
  templateUrl: './markdown-editor.html',
  styleUrl: './markdown-editor.css',
})
export class MarkdownEditor {
  readonly markdown = model('');

  private readonly textareaRef = viewChild<ElementRef<HTMLTextAreaElement>>('textarea');
  private readonly wysiwygRef = viewChild<ElementRef<HTMLDivElement>>('wysiwyg');

  protected readonly viewMode = signal<EditorViewMode>('wysiwyg');
  protected readonly toolbarGroups: ToolbarGroup[] = [
    {
      items: [
        { action: 'bold', label: 'Bold', icon: 'B' },
        { action: 'italic', label: 'Italic', icon: 'I' },
        { action: 'strikethrough', label: 'Strikethrough', icon: 'S' },
      ],
    },
    {
      items: [
        { action: 'heading1', label: 'Heading 1', icon: 'H1' },
        { action: 'heading2', label: 'Heading 2', icon: 'H2' },
        { action: 'heading3', label: 'Heading 3', icon: 'H3' },
      ],
    },
    {
      items: [
        { action: 'bulletList', label: 'Bullet list', icon: '•' },
        { action: 'orderedList', label: 'Numbered list', icon: '1.' },
        { action: 'blockquote', label: 'Blockquote', icon: '❝' },
      ],
    },
    {
      items: [
        { action: 'codeBlock', label: 'Code block', icon: '</>' },
        { action: 'link', label: 'Link', icon: '🔗' },
        { action: 'horizontalRule', label: 'Horizontal rule', icon: '—' },
      ],
    },
  ];

  private lastRenderedMarkdown = '';

  constructor() {
    effect(() => {
      const markdown = this.markdown();
      const mode = this.viewMode();

      if (mode !== 'wysiwyg' || markdown === this.lastRenderedMarkdown) {
        return;
      }

      const editor = this.wysiwygRef()?.nativeElement;
      if (!editor) {
        return;
      }

      editor.innerHTML = markdownToHtml(markdown);
      this.lastRenderedMarkdown = markdown;
    });
  }

  protected setViewMode(mode: EditorViewMode): void {
    if (mode === this.viewMode()) {
      return;
    }

    if (mode === 'code') {
      this.syncMarkdownFromWysiwyg();
      this.viewMode.set('code');
      return;
    }

    this.lastRenderedMarkdown = this.markdown();
    this.viewMode.set('wysiwyg');
  }

  protected onCodeInput(event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.lastRenderedMarkdown = value;
    this.markdown.set(value);
  }

  protected onWysiwygInput(): void {
    this.syncMarkdownFromWysiwyg();
  }

  protected applyFormat(action: MarkdownFormatAction): void {
    if (this.viewMode() === 'code') {
      this.applyCodeFormat(action);
      return;
    }

    this.applyWysiwygFormat(action);
    this.syncMarkdownFromWysiwyg();
  }

  private applyCodeFormat(action: MarkdownFormatAction): void {
    const textarea = this.textareaRef()?.nativeElement;
    if (!textarea) {
      return;
    }

    const result = applyMarkdownFormat(
      this.markdown(),
      textarea.selectionStart,
      textarea.selectionEnd,
      action,
    );

    this.lastRenderedMarkdown = result.value;
    this.markdown.set(result.value);
    textarea.focus();
    textarea.setSelectionRange(result.selectionStart, result.selectionEnd);
  }

  private applyWysiwygFormat(action: MarkdownFormatAction): void {
    const editor = this.wysiwygRef()?.nativeElement;
    if (!editor) {
      return;
    }

    editor.focus();

    switch (action) {
      case 'bold':
        document.execCommand('bold');
        break;
      case 'italic':
        document.execCommand('italic');
        break;
      case 'strikethrough':
        document.execCommand('strikeThrough');
        break;
      case 'heading1':
        document.execCommand('formatBlock', false, 'h1');
        break;
      case 'heading2':
        document.execCommand('formatBlock', false, 'h2');
        break;
      case 'heading3':
        document.execCommand('formatBlock', false, 'h3');
        break;
      case 'bulletList':
        document.execCommand('insertUnorderedList');
        break;
      case 'orderedList':
        document.execCommand('insertOrderedList');
        break;
      case 'blockquote':
        document.execCommand('formatBlock', false, 'blockquote');
        break;
      case 'codeBlock':
        document.execCommand('formatBlock', false, 'pre');
        break;
      case 'link': {
        const url = window.prompt('Link URL');
        if (url?.trim()) {
          document.execCommand('createLink', false, url.trim());
        }
        break;
      }
      case 'horizontalRule':
        document.execCommand('insertHorizontalRule');
        break;
    }
  }

  private syncMarkdownFromWysiwyg(): void {
    const editor = this.wysiwygRef()?.nativeElement;
    if (!editor) {
      return;
    }

    const markdown = htmlToMarkdown(editor.innerHTML);
    this.lastRenderedMarkdown = markdown;
    this.markdown.set(markdown);
  }
}
