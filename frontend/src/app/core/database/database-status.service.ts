import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { catchError, map, of, switchMap, timer } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  DatabaseHealthResponse,
  DbConnectionStatus,
} from './database-status.models';

@Injectable({ providedIn: 'root' })
export class DatabaseStatusService {
  private readonly http = inject(HttpClient);

  private readonly status = signal<DbConnectionStatus>('checking');

  readonly connectionStatus = this.status.asReadonly();

  readonly statusLabel = computed(() => {
    switch (this.status()) {
      case 'connected':
        return 'DB connected';
      case 'disconnected':
        return 'DB offline';
      default:
        return 'DB checking';
    }
  });

  constructor() {
    timer(0, 15_000)
      .pipe(switchMap(() => this.checkHealth()))
      .subscribe((nextStatus) => this.status.set(nextStatus));
  }

  private checkHealth() {
    return this.http
      .get<DatabaseHealthResponse>(`${environment.apiBaseUrl}/health/db`, {
        observe: 'response',
      })
      .pipe(
        map((response) =>
          response.status === 200 && response.body?.status === 'connected'
            ? ('connected' as const)
            : ('disconnected' as const),
        ),
        catchError(() => of('disconnected' as const)),
      );
  }
}
