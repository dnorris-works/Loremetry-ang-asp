import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, of } from 'rxjs';

import { AdminApiService } from '../../../core/admin/admin-api.service';
import { SqlQueryResult } from '../../../core/admin/admin.models';

@Component({
  selector: 'app-admin-console',
  templateUrl: './admin-console.html',
  styleUrl: './admin-console.css',
})
export class AdminConsole {
  private readonly adminApi = inject(AdminApiService);

  protected readonly sql = signal('SELECT schema_name FROM information_schema.schemata ORDER BY schema_name;');
  protected readonly result = signal<SqlQueryResult | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isRunning = signal(false);

  protected runSql(): void {
    const query = this.sql().trim();
    if (!query) {
      this.errorMessage.set('Enter a SQL statement to run.');
      this.result.set(null);
      return;
    }

    this.isRunning.set(true);
    this.errorMessage.set(null);
    this.result.set(null);

    this.adminApi
      .executeSql(query)
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.message === 'string'
              ? error.error.message
              : 'Failed to execute SQL.';
          this.errorMessage.set(message);
          return of(null);
        }),
      )
      .subscribe((result) => {
        this.isRunning.set(false);

        if (!result) {
          return;
        }

        this.result.set(result);
      });
  }

  protected onKeydown(event: KeyboardEvent): void {
    if ((event.metaKey || event.ctrlKey) && event.key === 'Enter') {
      event.preventDefault();
      this.runSql();
    }
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
