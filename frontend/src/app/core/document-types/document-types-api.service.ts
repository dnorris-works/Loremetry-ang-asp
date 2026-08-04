import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { DocumentType } from './document-types.models';

@Injectable({ providedIn: 'root' })
export class DocumentTypesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/document-types`;

  listDocumentTypes(parent?: 'story' | 'series'): Observable<DocumentType[]> {
    const params = parent ? { parent } : undefined;
    return this.http.get<DocumentType[]>(this.baseUrl, { params });
  }
}
