import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ColumnInfo,
  PlatformConnectionTestResult,
  PlatformSettings,
  SchemaInfo,
  SchemaObject,
  SqlQueryResult,
  TestPlatformSettingsRequest,
  UpdatePlatformSettingsRequest,
} from './admin.models';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/admin`;

  getSchemas(): Observable<SchemaInfo[]> {
    return this.http.get<SchemaInfo[]>(`${this.baseUrl}/schema/schemas`);
  }

  getSchemaObjects(schemaName: string): Observable<SchemaObject[]> {
    return this.http.get<SchemaObject[]>(`${this.baseUrl}/schema/${schemaName}/objects`);
  }

  getObjectColumns(schemaName: string, objectName: string): Observable<ColumnInfo[]> {
    return this.http.get<ColumnInfo[]>(
      `${this.baseUrl}/schema/${schemaName}/objects/${objectName}/columns`,
    );
  }

  getObjectData(
    schemaName: string,
    objectName: string,
    limit = 100,
  ): Observable<SqlQueryResult> {
    return this.http.get<SqlQueryResult>(
      `${this.baseUrl}/schema/${schemaName}/objects/${objectName}/data`,
      { params: { limit } },
    );
  }

  executeSql(sql: string): Observable<SqlQueryResult> {
    return this.http.post<SqlQueryResult>(`${this.baseUrl}/sql`, { sql });
  }

  getPlatformSettings(): Observable<PlatformSettings> {
    return this.http.get<PlatformSettings>(`${this.baseUrl}/platform-settings`);
  }

  updatePlatformSettings(settings: UpdatePlatformSettingsRequest): Observable<PlatformSettings> {
    return this.http.put<PlatformSettings>(`${this.baseUrl}/platform-settings`, settings);
  }

  testPlatformSettings(settings: TestPlatformSettingsRequest): Observable<PlatformConnectionTestResult> {
    return this.http.post<PlatformConnectionTestResult>(`${this.baseUrl}/platform-settings/test`, settings);
  }
}
