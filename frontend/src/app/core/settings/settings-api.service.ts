import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AppSetting, AppSettingKey } from './app-settings.models';

@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/settings`;

  getSettings(): Observable<AppSetting[]> {
    return this.http.get<AppSetting[]>(this.baseUrl);
  }

  updateSetting(key: AppSettingKey, value: string): Observable<AppSetting> {
    return this.http.put<AppSetting>(`${this.baseUrl}/${key}`, { value });
  }
}
