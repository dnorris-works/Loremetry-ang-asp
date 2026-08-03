import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CollectionDetail,
  CollectionEntry,
  CollectionField,
  CollectionSummary,
  ColumnInfo,
  CreateCollectionRequest,
  CreateFieldRequest,
  SchemaInfo,
  SchemaObject,
  SqlQueryResult,
} from './admin.models';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/admin`;

  getCollections(): Observable<CollectionSummary[]> {
    return this.http.get<CollectionSummary[]>(`${this.baseUrl}/collections`);
  }

  getCollection(id: string): Observable<CollectionDetail> {
    return this.http.get<CollectionDetail>(`${this.baseUrl}/collections/${id}`);
  }

  createCollection(request: CreateCollectionRequest): Observable<CollectionDetail> {
    return this.http.post<CollectionDetail>(`${this.baseUrl}/collections`, request);
  }

  deleteCollection(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/collections/${id}`);
  }

  createField(collectionId: string, request: CreateFieldRequest): Observable<CollectionField> {
    return this.http.post<CollectionField>(
      `${this.baseUrl}/collections/${collectionId}/fields`,
      request,
    );
  }

  deleteField(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/fields/${id}`);
  }

  getEntries(collectionId: string): Observable<CollectionEntry[]> {
    return this.http.get<CollectionEntry[]>(
      `${this.baseUrl}/collections/${collectionId}/entries`,
    );
  }

  createEntry(
    collectionId: string,
    values: Record<string, unknown>,
  ): Observable<CollectionEntry> {
    return this.http.post<CollectionEntry>(
      `${this.baseUrl}/collections/${collectionId}/entries`,
      { values },
    );
  }

  updateEntry(id: string, values: Record<string, unknown>): Observable<CollectionEntry> {
    return this.http.put<CollectionEntry>(`${this.baseUrl}/entries/${id}`, { values });
  }

  deleteEntry(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/entries/${id}`);
  }

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

  executeSql(sql: string): Observable<SqlQueryResult> {
    return this.http.post<SqlQueryResult>(`${this.baseUrl}/sql`, { sql });
  }
}
