import { Injectable, effect, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, of, tap } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { APP_SETTING_KEYS, AppSettingKey } from './app-settings.models';
import { SettingsApiService } from './settings-api.service';

@Injectable({ providedIn: 'root' })
export class AppSettingsService {
  private readonly settingsApi = inject(SettingsApiService);
  private readonly auth = inject(AuthService);

  readonly values = signal<Record<string, string>>({});
  readonly isLoaded = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (this.auth.enteredApp()) {
        this.loadSettings();
        return;
      }

      this.reset();
    });
  }

  get(key: AppSettingKey): string | undefined {
    return this.values()[key];
  }

  loadSettings(): void {
    this.loadError.set(null);
    this.isLoaded.set(false);

    this.settingsApi
      .getSettings()
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.message === 'string'
              ? error.error.message
              : 'Failed to load settings from the database.';
          this.loadError.set(message);
          this.isLoaded.set(true);
          return of(null);
        }),
      )
      .subscribe((settings) => {
        if (!settings) {
          return;
        }

        this.values.set(
          Object.fromEntries(settings.map((setting) => [setting.key, setting.value])),
        );
        this.isLoaded.set(true);
      });
  }

  updateSetting(key: AppSettingKey, value: string): void {
    this.saveError.set(null);

    this.settingsApi
      .updateSetting(key, value)
      .pipe(
        tap((setting) => {
          this.values.update((current) => ({
            ...current,
            [setting.key]: setting.value,
          }));
        }),
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.message === 'string'
              ? error.error.message
              : `Failed to save setting "${key}".`;
          this.saveError.set(message);
          return of(null);
        }),
      )
      .subscribe();
  }

  reset(): void {
    this.values.set({});
    this.isLoaded.set(false);
    this.loadError.set(null);
    this.saveError.set(null);
  }
}

export { APP_SETTING_KEYS };
