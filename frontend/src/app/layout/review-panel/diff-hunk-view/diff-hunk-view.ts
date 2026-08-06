import { Component, input, signal } from '@angular/core';

import { ReviewSuggestionHunk } from '../../../core/review/review.models';

@Component({
  selector: 'app-diff-hunk-view',
  templateUrl: './diff-hunk-view.html',
  styleUrl: './diff-hunk-view.css',
})
export class DiffHunkView {
  readonly hunk = input.required<ReviewSuggestionHunk>();

  protected readonly copyMessage = signal<string | null>(null);

  protected severityLabel(severity: ReviewSuggestionHunk['severity']): string {
    switch (severity) {
      case 'high':
        return 'High';
      case 'medium':
        return 'Medium';
      default:
        return 'Low';
    }
  }

  protected async copySuggestion(): Promise<void> {
    await this.copyText(this.hunk().suggestedReplacement, 'Suggestion copied.');
  }

  protected async copyInsertion(): Promise<void> {
    const insertion = this.hunk()
      .segments.filter((segment) => segment.type === 'insert')
      .map((segment) => segment.text)
      .join('');

    await this.copyText(insertion, 'Insertion copied.');
  }

  private async copyText(text: string, successMessage: string): Promise<void> {
    if (!text.trim()) {
      this.flashCopyMessage('Nothing to copy.');
      return;
    }

    try {
      await navigator.clipboard.writeText(text);
      this.flashCopyMessage(successMessage);
    } catch {
      this.flashCopyMessage('Copy failed.');
    }
  }

  private flashCopyMessage(message: string): void {
    this.copyMessage.set(message);
    window.setTimeout(() => {
      if (this.copyMessage() === message) {
        this.copyMessage.set(null);
      }
    }, 2200);
  }
}
