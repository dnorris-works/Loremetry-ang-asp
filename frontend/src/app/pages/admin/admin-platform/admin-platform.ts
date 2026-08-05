import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, of } from 'rxjs';

import { AdminApiService } from '../../../core/admin/admin-api.service';
import {
  PlatformServiceStatus,
  PlatformServiceTestResult,
  PlatformSettings,
} from '../../../core/admin/admin.models';

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

  protected readonly serviceStatuses = signal<PlatformServiceStatus[]>([]);

  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isTesting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly saveMessage = signal<string | null>(null);
  protected readonly testResults = signal<PlatformServiceTestResult[]>([]);

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
        this.loadSettings();
      });
  }

  protected statusLabel(status: PlatformServiceStatus): string {
    switch (status.state) {
      case 'connected':
        return 'Connected';
      case 'failed':
        return 'Failed';
      case 'never_tested':
        return 'Not tested';
      case 'ready':
        return 'Ready';
      case 'partial':
        return 'Partial';
      case 'not_imported':
        return 'Not imported';
      case 'not_configured':
      default:
        return 'Not configured';
    }
  }

  protected statusClass(status: PlatformServiceStatus): string {
    switch (status.state) {
      case 'connected':
      case 'ready':
        return 'admin-platform__status-ok';
      case 'failed':
        return 'admin-platform__status-fail';
      case 'partial':
      case 'never_tested':
        return 'admin-platform__status-warn';
      case 'not_imported':
      case 'not_configured':
      default:
        return 'admin-platform__status-missing';
    }
  }

  protected formatTestedAt(testedAt: string | null): string | null {
    if (!testedAt) {
      return null;
    }

    const date = new Date(testedAt);
    return Number.isNaN(date.getTime()) ? null : date.toLocaleString();
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
    this.serviceStatuses.set(settings.serviceStatuses ?? []);
  }
}
