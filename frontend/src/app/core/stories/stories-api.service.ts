import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CreateStoryRequest, Story, StoryDetail, UpdateStoryRequest } from './story.models';

@Injectable({ providedIn: 'root' })
export class StoriesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/stories`;

  listStories(): Observable<Story[]> {
    return this.http.get<Story[]>(this.baseUrl);
  }

  getStory(id: number): Observable<StoryDetail> {
    return this.http.get<StoryDetail>(`${this.baseUrl}/${id}`);
  }

  createStory(request: CreateStoryRequest): Observable<Story> {
    return this.http.post<Story>(this.baseUrl, request);
  }

  updateStory(id: number, request: UpdateStoryRequest): Observable<Story> {
    return this.http.put<Story>(`${this.baseUrl}/${id}`, request);
  }
}
