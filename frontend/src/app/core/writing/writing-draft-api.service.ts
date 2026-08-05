import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UpsertWritingDraftRequest, WritingDraft } from './writing-draft.models';

@Injectable({ providedIn: 'root' })
export class WritingDraftApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/writing/drafts`;

  getDraft(draftKey: string): Observable<WritingDraft | null> {
    return this.http.get<WritingDraft>(this.baseUrl, { params: { draftKey } }).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 404) {
          return of(null);
        }

        throw error;
      }),
    );
  }

  upsertDraft(request: UpsertWritingDraftRequest): Observable<WritingDraft> {
    return this.http.put<WritingDraft>(this.baseUrl, request);
  }

  deleteDraft(draftKey: string): Observable<void> {
    return this.http.delete<void>(this.baseUrl, { params: { draftKey } });
  }
}
