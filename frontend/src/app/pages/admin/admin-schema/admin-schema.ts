import { Component, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';

import { AdminApiService } from '../../../core/admin/admin-api.service';
import { ColumnInfo, SchemaInfo, SchemaObject, SqlQueryResult } from '../../../core/admin/admin.models';

@Component({
  selector: 'app-admin-schema',
  templateUrl: './admin-schema.html',
  styleUrl: './admin-schema.css',
})
export class AdminSchema {
  private readonly adminApi = inject(AdminApiService);

  protected readonly schemas = signal<SchemaInfo[]>([]);
  protected readonly objects = signal<SchemaObject[]>([]);
  protected readonly columns = signal<ColumnInfo[]>([]);
  protected readonly rows = signal<SqlQueryResult | null>(null);
  protected readonly selectedSchema = signal<string | null>(null);
  protected readonly selectedObject = signal<SchemaObject | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    this.loadSchemas();
  }

  protected loadSchemas(): void {
    this.errorMessage.set(null);

    this.adminApi
      .getSchemas()
      .pipe(catchError(() => of(null)))
      .subscribe((schemas) => {
        if (!schemas) {
          this.errorMessage.set('Failed to load database schemas.');
          return;
        }

        this.schemas.set(schemas);
      });
  }

  protected selectSchema(schemaName: string): void {
    this.selectedSchema.set(schemaName);
    this.selectedObject.set(null);
    this.columns.set([]);
    this.rows.set(null);
    this.errorMessage.set(null);

    this.adminApi
      .getSchemaObjects(schemaName)
      .pipe(catchError(() => of(null)))
      .subscribe((objects) => {
        if (!objects) {
          this.errorMessage.set(`Failed to load objects for schema "${schemaName}".`);
          this.objects.set([]);
          return;
        }

        this.objects.set(objects);
      });
  }

  protected selectObject(object: SchemaObject): void {
    this.selectedObject.set(object);
    this.columns.set([]);
    this.rows.set(null);

    if (object.type !== 'TABLE' && object.type !== 'VIEW') {
      return;
    }

    const schemaName = this.selectedSchema();
    if (!schemaName) {
      return;
    }

    this.errorMessage.set(null);

    this.adminApi
      .getObjectColumns(schemaName, object.name)
      .pipe(catchError(() => of(null)))
      .subscribe((columns) => {
        if (!columns) {
          this.errorMessage.set(`Failed to load columns for "${schemaName}.${object.name}".`);
          return;
        }

        this.columns.set(columns);
      });

    this.adminApi
      .getObjectData(schemaName, object.name)
      .pipe(catchError(() => of(null)))
      .subscribe((data) => {
        if (!data) {
          this.errorMessage.set(`Failed to load data for "${schemaName}.${object.name}".`);
          return;
        }

        this.rows.set(data);
      });
  }

  protected isColumnObject(object: SchemaObject): boolean {
    return object.type === 'TABLE' || object.type === 'VIEW';
  }

  protected formatCell(value: unknown): string {
    if (value === null || value === undefined) {
      return 'NULL';
    }

    if (typeof value === 'object') {
      return JSON.stringify(value);
    }

    return String(value);
  }
}
