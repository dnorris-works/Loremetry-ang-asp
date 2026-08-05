import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, of } from 'rxjs';

import { AdminApiService } from '../../../core/admin/admin-api.service';
import { PlatformServiceTestResult, PlatformSettings } from '../../../core/admin/admin.models';

@Component({
  selector: 'app-admin-platform',
  imports: [FormsModule],
  templateUrl: './admin-platform.html',
  styleUrl: './admin-platform.css',
})
export class AdminPlatform implements OnInit {
  private readonly adminApi = inject(AdminApiService);

  protected readonly anthropicApiKey = signal('');
  protected readonly tokenmixApiKey = signal('');
  protected readonly canopyApiKey = signal('');
  protected readonly dataForSeoLogin = signal('');
  protected readonly dataForSeoPassword = signal('');
  protected readonly defaultProvider = signal('tokenmix');
  protected readonly defaultModel = signal('');

  protected readonly configured = signal<Pick<
    PlatformSettings,
    'anthropicConfigured' | 'tokenmixConfigured' | 'canopyConfigured' | 'dataForSeoConfigured'
  > | null>(null);

  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isTesting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly saveMessage = signal<string | null>(null);
  protected readonly testResults = signal<PlatformServiceTestResult[]>([]);

  protected readonly serviceStatuses = computed(() => {
    const status = this.configured();
    if (!status) {
      return [];
    }

    return [
      { label: 'TokenMix AI', configured: status.tokenmixConfigured },
      { label: 'Anthropic', configured: status.anthropicConfigured },
      { label: 'Canopy', configured: status.canopyConfigured },
      { label: 'DataForSEO', configured: status.dataForSeoConfigured },
    ];
  });

  ngOnInit(): void {
    this.loadSettings();
  }

  protected loadSettings(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.saveMessage.set(null);

    this.adminApi
      .getPlatformSettings()
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.message === 'string'
              ? error.error.message
              : 'Failed to load platform settings.';
          this.errorMessage.set(message);
          return of(null);
        }),
      )
      .subscribe((settings) => {
        this.isLoading.set(false);

        if (!settings) {
          return;
        }

        this.applySettings(settings);
      });
  }

  protected saveSettings(event: Event): void {
    event.preventDefault();
    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.saveMessage.set(null);

    this.adminApi
      .updatePlatformSettings(this.currentSettingsPayload())
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.message === 'string'
              ? error.error.message
              : 'Failed to save platform settings.';
          this.errorMessage.set(message);
          return of(null);
        }),
      )
      .subscribe((settings) => {
        this.isSaving.set(false);

        if (!settings) {
          return;
        }

        this.applySettings(settings);
        this.saveMessage.set('Platform settings saved.');
      });
  }

  protected testConnections(): void {
    this.isTesting.set(true);
    this.errorMessage.set(null);
    this.saveMessage.set(null);
    this.testResults.set([]);

    this.adminApi
      .testPlatformSettings(this.currentSettingsPayload())
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.message === 'string'
              ? error.error.message
              : 'Failed to test platform connections.';
          this.errorMessage.set(message);
          return of(null);
        }),
      )
      .subscribe((result) => {
        this.isTesting.set(false);

        if (!result) {
          return;
        }

        this.testResults.set(result.results);
      });
  }

  private currentSettingsPayload() {
    return {
      anthropicApiKey: this.anthropicApiKey(),
      tokenmixApiKey: this.tokenmixApiKey(),
      canopyApiKey: this.canopyApiKey(),
      dataForSeoLogin: this.dataForSeoLogin(),
      dataForSeoPassword: this.dataForSeoPassword(),
      defaultProvider: this.defaultProvider(),
      defaultModel: this.defaultModel(),
    };
  }

  private applySettings(settings: PlatformSettings): void {
    this.anthropicApiKey.set(settings.anthropicApiKey);
    this.tokenmixApiKey.set(settings.tokenmixApiKey);
    this.canopyApiKey.set(settings.canopyApiKey);
    this.dataForSeoLogin.set(settings.dataForSeoLogin);
    this.dataForSeoPassword.set(settings.dataForSeoPassword);
    this.defaultProvider.set(settings.defaultProvider);
    this.defaultModel.set(settings.defaultModel);
    this.configured.set({
      anthropicConfigured: settings.anthropicConfigured,
      tokenmixConfigured: settings.tokenmixConfigured,
      canopyConfigured: settings.canopyConfigured,
      dataForSeoConfigured: settings.dataForSeoConfigured,
    });
  }
}
