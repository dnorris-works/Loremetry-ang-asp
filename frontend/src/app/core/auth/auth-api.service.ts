import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthConfig, AuthSession, MeResponse } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/auth`;

  getConfig(): Observable<AuthConfig> {
    return this.http.get<AuthConfig>(`${this.baseUrl}/config`);
  }

  getSession(): Observable<AuthSession> {
    return this.http.get<AuthSession>(`${this.baseUrl}/session`);
  }

  getMe(): Observable<MeResponse> {
    return this.http.get<MeResponse>(`${this.baseUrl}/me`);
  }
}
