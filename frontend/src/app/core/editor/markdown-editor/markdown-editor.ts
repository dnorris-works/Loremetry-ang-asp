import {
  Component,
  ElementRef,
  OnDestroy,
  effect,
  model,
  signal,
  viewChild,
} from '@angular/core';
import { Editor } from '@tiptap/core';
import Link from '@tiptap/extension-link';
import { Markdown } from '@tiptap/markdown';
import StarterKit from '@tiptap/starter-kit';
import { TiptapEditorDirective } from 'ngx-tiptap';

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
  imports: [TiptapEditorDirective],
  templateUrl: './markdown-editor.html',
  styleUrl: './markdown-editor.css',
})
export class MarkdownEditor implements OnDestroy {
  readonly markdown = model('');

  protected readonly editor: Editor;
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

  private readonly textareaRef = viewChild<ElementRef<HTMLTextAreaElement>>('textarea');

  constructor() {
    this.editor = new Editor({
      extensions: [
        StarterKit,
        Link.configure({
          openOnClick: false,
          autolink: true,
        }),
        Markdown,
      ],
      content: '',
      contentType: 'markdown',
      editorProps: {
        attributes: {
          class: 'markdown-editor__prosemirror tiptap',
        },
      },
      onUpdate: ({ editor }) => this.handleEditorUpdate(editor),
    });

    effect(() => {
      const markdown = this.markdown();
      const mode = this.viewMode();

      if (mode === 'code') {
        this.syncCodeTextarea(markdown);
        return;
      }

      this.syncEditorMarkdown(markdown);
    });
  }

  ngOnDestroy(): void {
    this.editor.destroy();
  }

  protected setViewMode(mode: EditorViewMode): void {
    if (mode === this.viewMode()) {
      return;
    }

    if (mode === 'code') {
      this.markdown.set(this.editor.getMarkdown());
      this.viewMode.set('code');
      return;
    }

    this.editor.commands.setContent(this.markdown(), {
      contentType: 'markdown',
      emitUpdate: false,
    });
    this.viewMode.set('wysiwyg');
  }

  protected onCodeInput(event: Event): void {
    this.markdown.set((event.target as HTMLTextAreaElement).value);
  }

  protected applyFormat(action: MarkdownFormatAction): void {
    if (this.viewMode() === 'code') {
      this.applyCodeFormat(action);
      return;
    }

    this.applyVisualFormat(action);
  }

  private handleEditorUpdate(editor: Editor): void {
    if (this.viewMode() !== 'wysiwyg') {
      return;
    }

    const nextMarkdown = editor.getMarkdown();
    if (nextMarkdown !== this.markdown()) {
      this.markdown.set(nextMarkdown);
    }
  }

  private syncEditorMarkdown(markdown: string): void {
    const currentMarkdown = this.editor.getMarkdown();
    if (currentMarkdown === markdown) {
      return;
    }

    this.editor.commands.setContent(markdown, {
      contentType: 'markdown',
      emitUpdate: false,
    });
  }

  private syncCodeTextarea(markdown: string): void {
    const textarea = this.textareaRef()?.nativeElement;
    if (!textarea || textarea.value === markdown) {
      return;
    }

    textarea.value = markdown;
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

    this.markdown.set(result.value);
    textarea.focus();
    textarea.setSelectionRange(result.selectionStart, result.selectionEnd);
  }

  private applyVisualFormat(action: MarkdownFormatAction): void {
    const chain = this.editor.chain().focus();

    switch (action) {
      case 'bold':
        chain.toggleBold().run();
        break;
      case 'italic':
        chain.toggleItalic().run();
        break;
      case 'strikethrough':
        chain.toggleStrike().run();
        break;
      case 'heading1':
        chain.toggleHeading({ level: 1 }).run();
        break;
      case 'heading2':
        chain.toggleHeading({ level: 2 }).run();
        break;
      case 'heading3':
        chain.toggleHeading({ level: 3 }).run();
        break;
      case 'bulletList':
        chain.toggleBulletList().run();
        break;
      case 'orderedList':
        chain.toggleOrderedList().run();
        break;
      case 'blockquote':
        chain.toggleBlockquote().run();
        break;
      case 'codeBlock':
        chain.toggleCodeBlock().run();
        break;
      case 'link': {
        const url = window.prompt('Link URL');
        if (url?.trim()) {
          chain.setLink({ href: url.trim() }).run();
        }
        break;
      }
      case 'horizontalRule':
        chain.setHorizontalRule().run();
        break;
    }
  }
}
