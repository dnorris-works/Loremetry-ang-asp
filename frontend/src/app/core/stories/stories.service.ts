import { Injectable, signal } from '@angular/core';

import { CreateStoryRequest, Story } from './story.models';

@Injectable({ providedIn: 'root' })
export class StoriesService {
  readonly stories = signal<Story[]>([]);
  readonly isAddPanelOpen = signal(false);
  readonly recentManuscriptFiles = signal<string[]>([]);
  readonly recentBibleFiles = signal<string[]>([]);

  openAddPanel(): void {
    this.isAddPanelOpen.set(true);
  }

  closeAddPanel(): void {
    this.isAddPanelOpen.set(false);
  }

  addStory(request: CreateStoryRequest): Story {
    const story: Story = {
      id: crypto.randomUUID(),
      name: request.name.trim(),
      manuscriptFiles: this.normalizeFiles(request.manuscriptFiles),
      bibleFiles: this.normalizeFiles(request.bibleFiles),
      createdAt: new Date().toISOString(),
    };

    this.stories.update((stories) => [story, ...stories]);
    this.rememberManuscriptFiles(story.manuscriptFiles);
    this.rememberBibleFiles(story.bibleFiles);
    this.closeAddPanel();

    return story;
  }

  rememberManuscriptFiles(files: string[]): void {
    this.rememberFiles(files, this.recentManuscriptFiles);
  }

  rememberBibleFiles(files: string[]): void {
    this.rememberFiles(files, this.recentBibleFiles);
  }

  private rememberFiles(files: string[], target: ReturnType<typeof signal<string[]>>): void {
    for (const file of files) {
      const trimmed = file.trim();
      if (!trimmed) {
        continue;
      }

      target.update((paths) => [trimmed, ...paths.filter((existing) => existing !== trimmed)]);
    }
  }

  private normalizeFiles(files: string[]): string[] {
    return files.map((file) => file.trim()).filter(Boolean);
  }
}
