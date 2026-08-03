import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, of, switchMap } from 'rxjs';

import { AdminApiService } from '../../../core/admin/admin-api.service';
import {
  CollectionDetail,
  CollectionEntry,
  CollectionField,
  FIELD_TYPES,
  FieldType,
} from '../../../core/admin/admin.models';

@Component({
  selector: 'app-admin-collection-detail',
  imports: [RouterLink, DatePipe],
  templateUrl: './admin-collection-detail.html',
  styleUrl: './admin-collection-detail.css',
})
export class AdminCollectionDetail {
  private readonly adminApi = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);

  protected readonly fieldTypes = FIELD_TYPES;
  protected readonly collection = signal<CollectionDetail | null>(null);
  protected readonly entries = signal<CollectionEntry[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly fieldName = signal('');
  protected readonly fieldKey = signal('');
  protected readonly fieldType = signal<FieldType>('Text');
  protected readonly fieldRequired = signal(false);
  protected readonly fieldOrder = signal(0);

  protected readonly editingEntryId = signal<string | null>(null);
  protected readonly entryValues = signal<Record<string, string>>({});

  protected readonly sortedFields = computed(
    () => [...(this.collection()?.fields ?? [])].sort((a, b) => a.displayOrder - b.displayOrder),
  );

  constructor() {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (!id) {
            return of(null);
          }

          this.isLoading.set(true);
          return this.adminApi.getCollection(id).pipe(catchError(() => of(null)));
        }),
      )
      .subscribe((collection) => {
        this.isLoading.set(false);
        if (!collection) {
          this.errorMessage.set('Collection not found.');
          return;
        }

        this.collection.set(collection);
        this.fieldOrder.set(collection.fields.length);
        this.loadEntries(collection.id);
        this.resetEntryForm();
      });
  }

  protected loadEntries(collectionId: string): void {
    this.adminApi
      .getEntries(collectionId)
      .pipe(catchError(() => of([])))
      .subscribe((entries) => this.entries.set(entries));
  }

  protected reloadCollection(): void {
    const collection = this.collection();
    if (!collection) {
      return;
    }

    this.adminApi
      .getCollection(collection.id)
      .pipe(catchError(() => of(null)))
      .subscribe((updated) => {
        if (updated) {
          this.collection.set(updated);
        }
      });
  }

  protected addField(): void {
    const collection = this.collection();
    if (!collection) {
      return;
    }

    this.adminApi
      .createField(collection.id, {
        name: this.fieldName().trim(),
        fieldKey: this.fieldKey().trim(),
        fieldType: this.fieldType(),
        isRequired: this.fieldRequired(),
        displayOrder: this.fieldOrder(),
      })
      .pipe(catchError(() => of(null)))
      .subscribe((field) => {
        if (!field) {
          this.errorMessage.set('Failed to add field.');
          return;
        }

        this.fieldName.set('');
        this.fieldKey.set('');
        this.fieldType.set('Text');
        this.fieldRequired.set(false);
        this.fieldOrder.update((order) => order + 1);
        this.reloadCollection();
      });
  }

  protected deleteField(field: CollectionField): void {
    if (!confirm(`Delete field "${field.name}"?`)) {
      return;
    }

    this.adminApi
      .deleteField(field.id)
      .pipe(catchError(() => of(null)))
      .subscribe(() => this.reloadCollection());
  }

  protected resetEntryForm(): void {
    const values: Record<string, string> = {};
    for (const field of this.sortedFields()) {
      values[field.fieldKey] = '';
    }

    this.editingEntryId.set(null);
    this.entryValues.set(values);
  }

  protected startEditEntry(entry: CollectionEntry): void {
    const values: Record<string, string> = {};
    for (const field of this.sortedFields()) {
      const raw = entry.values[field.fieldKey];
      values[field.fieldKey] = raw === null || raw === undefined ? '' : String(raw);
    }

    this.editingEntryId.set(entry.id);
    this.entryValues.set(values);
  }

  protected setEntryValue(fieldKey: string, value: string): void {
    this.entryValues.update((current) => ({ ...current, [fieldKey]: value }));
  }

  protected saveEntry(): void {
    const collection = this.collection();
    if (!collection) {
      return;
    }

    const payload = this.buildEntryPayload(collection.fields);
    const editingId = this.editingEntryId();

    const request$ = editingId
      ? this.adminApi.updateEntry(editingId, payload)
      : this.adminApi.createEntry(collection.id, payload);

    request$.pipe(catchError(() => of(null))).subscribe((entry) => {
      if (!entry) {
        this.errorMessage.set('Failed to save entry.');
        return;
      }

      this.loadEntries(collection.id);
      this.resetEntryForm();
    });
  }

  protected deleteEntry(entry: CollectionEntry): void {
    if (!confirm('Delete this entry?')) {
      return;
    }

    this.adminApi
      .deleteEntry(entry.id)
      .pipe(catchError(() => of(null)))
      .subscribe(() => {
        const collection = this.collection();
        if (collection) {
          this.loadEntries(collection.id);
        }

        if (this.editingEntryId() === entry.id) {
          this.resetEntryForm();
        }
      });
  }

  protected displayEntryValue(entry: CollectionEntry, field: CollectionField): string {
    const value = entry.values[field.fieldKey];
    if (value === null || value === undefined || value === '') {
      return '—';
    }

    return String(value);
  }

  protected onFieldNameInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.fieldName.set(value);

    if (!this.fieldKey()) {
      this.fieldKey.set(
        value
          .trim()
          .toLowerCase()
          .replace(/[^a-z0-9]+/g, '_')
          .replace(/^_+|_+$/g, ''),
      );
    }
  }

  private buildEntryPayload(fields: CollectionField[]): Record<string, unknown> {
    const values = this.entryValues();
    const payload: Record<string, unknown> = {};

    for (const field of fields) {
      const raw = values[field.fieldKey] ?? '';

      payload[field.fieldKey] = field.fieldType === 'Number'
        ? raw === '' ? null : Number(raw)
        : field.fieldType === 'Boolean'
          ? raw === 'true'
          : field.fieldType === 'Json'
            ? this.parseJsonValue(raw)
            : raw;
    }

    return payload;
  }

  private parseJsonValue(raw: string): unknown {
    if (!raw.trim()) {
      return null;
    }

    try {
      return JSON.parse(raw);
    } catch {
      this.errorMessage.set('Invalid JSON value.');
      return raw;
    }
  }
}
