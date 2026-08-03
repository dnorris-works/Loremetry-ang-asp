import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';

import { AdminApiService } from '../../../core/admin/admin-api.service';
import { CollectionSummary } from '../../../core/admin/admin.models';

@Component({
  selector: 'app-admin-collections',
  imports: [RouterLink],
  templateUrl: './admin-collections.html',
  styleUrl: './admin-collections.css',
})
export class AdminCollections {
  private readonly adminApi = inject(AdminApiService);
  private readonly router = inject(Router);

  protected readonly collections = signal<CollectionSummary[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly newName = signal('');
  protected readonly newSlug = signal('');
  protected readonly newDescription = signal('');

  constructor() {
    this.loadCollections();
  }

  protected loadCollections(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminApi
      .getCollections()
      .pipe(catchError(() => of(null)))
      .subscribe((collections) => {
        this.isLoading.set(false);
        if (!collections) {
          this.errorMessage.set('Failed to load collections.');
          return;
        }

        this.collections.set(collections);
      });
  }

  protected createCollection(): void {
    const name = this.newName().trim();
    const slug = this.newSlug().trim();

    if (!name || !slug) {
      this.errorMessage.set('Name and slug are required.');
      return;
    }

    this.adminApi
      .createCollection({
        name,
        slug,
        description: this.newDescription().trim() || null,
      })
      .pipe(catchError(() => of(null)))
      .subscribe((collection) => {
        if (!collection) {
          this.errorMessage.set('Failed to create collection.');
          return;
        }

        this.newName.set('');
        this.newSlug.set('');
        this.newDescription.set('');
        this.router.navigate(['/admin/collections', collection.id]);
      });
  }

  protected deleteCollection(id: string, name: string): void {
    if (!confirm(`Delete collection "${name}" and all of its fields and entries?`)) {
      return;
    }

    this.adminApi
      .deleteCollection(id)
      .pipe(catchError(() => of(null)))
      .subscribe((result) => {
        if (result === null) {
          this.errorMessage.set('Failed to delete collection.');
          return;
        }

        this.loadCollections();
      });
  }

  protected onNameInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.newName.set(value);

    if (!this.newSlug()) {
      this.newSlug.set(
        value
          .trim()
          .toLowerCase()
          .replace(/[^a-z0-9]+/g, '-')
          .replace(/^-+|-+$/g, ''),
      );
    }
  }
}
