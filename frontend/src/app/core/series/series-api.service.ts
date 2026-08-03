import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CreateSeriesRequest, Series } from './series.models';

@Injectable({ providedIn: 'root' })
export class SeriesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/series`;

  listSeries(): Observable<Series[]> {
    return this.http.get<Series[]>(this.baseUrl);
  }

  createSeries(request: CreateSeriesRequest): Observable<Series> {
    return this.http.post<Series>(this.baseUrl, request);
  }
}
