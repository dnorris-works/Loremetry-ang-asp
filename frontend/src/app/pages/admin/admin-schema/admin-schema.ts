import { Component, inject, signal } from '@angular/core';
import { catchError, of } from 'rxjs';

import { AdminApiService } from '../../../core/admin/admin-api.service';
import { ColumnInfo, TableInfo } from '../../../core/admin/admin.models';

@Component({
  selector: 'app-admin-schema',
  templateUrl: './admin-schema.html',
  styleUrl: './admin-schema.css',
})
export class AdminSchema {
  private readonly adminApi = inject(AdminApiService);

  protected readonly tables = signal<TableInfo[]>([]);
  protected readonly columns = signal<ColumnInfo[]>([]);
  protected readonly selectedTable = signal<string | null>(null);

  constructor() {
    this.loadTables();
  }

  protected loadTables(): void {
    this.adminApi
      .getTables()
      .pipe(catchError(() => of([])))
      .subscribe((tables) => this.tables.set(tables));
  }

  protected selectTable(tableName: string): void {
    this.selectedTable.set(tableName);
    this.adminApi
      .getColumns(tableName)
      .pipe(catchError(() => of([])))
      .subscribe((columns) => this.columns.set(columns));
  }
}
