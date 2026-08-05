import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface WritingAssistantChatRequest {
  userPrompt: string;
  augmentedPrompt: string;
}

export interface WritingAssistantChatResponse {
  text: string;
}

@Injectable({ providedIn: 'root' })
export class WritingAssistantApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/writing/assistant`;

  chat(request: WritingAssistantChatRequest): Observable<WritingAssistantChatResponse> {
    return this.http.post<WritingAssistantChatResponse>(`${this.baseUrl}/chat`, request);
  }
}
