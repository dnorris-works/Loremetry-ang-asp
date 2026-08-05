import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, of } from 'rxjs';

import { AdminApiService } from '../../../core/admin/admin-api.service';
import {
  CanopyOperationEstimate,
  CanopyPricingPlan,
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
  protected readonly canopyPricingPlan = signal('pay_as_you_go');
  protected readonly dataForSeoLogin = signal('');
  protected readonly dataForSeoPassword = signal('');
  protected readonly defaultProvider = signal('tokenmix');
  protected readonly defaultModel = signal('');

  protected readonly serviceStatuses = signal<PlatformServiceStatus[]>([]);

  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isTesting = signal(false);
  protected readonly isImportingWinningCat = signal(false);
  protected readonly isRemovingStale = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly saveMessage = signal<string | null>(null);
  protected readonly testResults = signal<PlatformServiceTestResult[]>([]);
  protected readonly winningCatImportMessage = signal<string | null>(null);
  protected readonly winningCatStaleMessage = signal<string | null>(null);
  protected readonly showStaleCleanup = signal(false);
  protected readonly lastWinningCatImportAt = signal<string | null>(null);
  protected readonly canopyPlans = signal<CanopyPricingPlan[]>([]);
  protected readonly canopyRequestsUsedThisMonth = signal(0);
  protected readonly canopyOperationEstimates = signal<CanopyOperationEstimate[]>([]);

  ngOnInit(): void {
    this.loadSettings();
    this.loadCanopyPricing();
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

  protected loadCanopyPricing(): void {
    this.adminApi
      .getCanopyPricing()
      .pipe(
        catchError(() => of(null)),
      )
      .subscribe((overview) => {
        if (!overview) {
          return;
        }

        this.canopyPlans.set(overview.plans);
        this.canopyRequestsUsedThisMonth.set(overview.requestsUsedThisMonth);
        if (overview.activePlanId) {
          this.canopyPricingPlan.set(overview.activePlanId);
        }
      });

    this.adminApi
      .getCanopyOperationEstimates()
      .pipe(
        catchError(() => of(null)),
      )
      .subscribe((estimates) => {
        if (estimates) {
          this.canopyOperationEstimates.set(estimates);
        }
      });
  }

  protected formatUsd(amount: number): string {
    if (amount <= 0) {
      return '$0.00';
    }

    if (amount < 0.01) {
      return '<$0.01';
    }

    return `$${amount.toFixed(2)}`;
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
        this.loadCanopyPricing();
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
        this.loadCanopyPricing();
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

  protected onWinningCatFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file) {
      return;
    }

    this.isImportingWinningCat.set(true);
    this.errorMessage.set(null);
    this.saveMessage.set(null);
    this.winningCatImportMessage.set('Importing…');
    this.winningCatStaleMessage.set(null);
    this.showStaleCleanup.set(false);

    this.adminApi
      .uploadWinningCatCsv(file)
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.error === 'string'
              ? error.error.error
              : typeof error.error?.message === 'string'
                ? error.error.message
                : 'Failed to import WinningCat CSV.';
          this.winningCatImportMessage.set(message);
          return of(null);
        }),
      )
      .subscribe((result) => {
        this.isImportingWinningCat.set(false);

        if (!result) {
          return;
        }

        if (!result.success) {
          this.winningCatImportMessage.set(result.error ?? 'Import failed.');
          return;
        }

        this.winningCatImportMessage.set(
          `Imported ${result.imported.toLocaleString()} categories. Skipped ${result.skippedOtherDepartment.toLocaleString()} (other department), ${result.skippedUnparseable.toLocaleString()} (unparseable).`,
        );
        this.lastWinningCatImportAt.set(result.importedAt);

        if (result.staleCount > 0) {
          this.showStaleCleanup.set(true);
          const word = result.staleCount === 1 ? 'y was' : 'ies were';
          this.winningCatStaleMessage.set(
            `${result.staleCount.toLocaleString()} categor${word} in the catalog from a previous import but missing from this one.`,
          );
        }

        this.loadSettings();
      });
  }

  protected removeStaleWinningCatCategories(): void {
    const since = this.lastWinningCatImportAt();
    if (!since) {
      return;
    }

    if (
      !confirm(
        'Remove stale categories from the catalog? Only reference data is affected.',
      )
    ) {
      return;
    }

    this.isRemovingStale.set(true);
    this.winningCatStaleMessage.set(null);

    this.adminApi
      .removeStaleWinningCatCategories(since)
      .pipe(
        catchError((error: HttpErrorResponse) => {
          const message =
            typeof error.error?.error === 'string'
              ? error.error.error
              : typeof error.error?.message === 'string'
                ? error.error.message
                : 'Failed to remove stale categories.';
          this.winningCatStaleMessage.set(message);
          return of(null);
        }),
      )
      .subscribe((result) => {
        this.isRemovingStale.set(false);

        if (!result) {
          return;
        }

        if (!result.success) {
          this.winningCatStaleMessage.set(result.error ?? 'Cleanup failed.');
          return;
        }

        const word = result.removed === 1 ? 'y' : 'ies';
        this.winningCatStaleMessage.set(
          `Removed ${result.removed.toLocaleString()} stale categor${word}.`,
        );
        this.showStaleCleanup.set(false);
        this.loadSettings();
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
      canopyPricingPlan: this.canopyPricingPlan(),
    };
  }

  private applySettings(settings: PlatformSettings): void {
    this.anthropicApiKey.set(settings.anthropicApiKey);
    this.tokenmixApiKey.set(settings.tokenmixApiKey);
    this.canopyApiKey.set(settings.canopyApiKey);
    this.canopyPricingPlan.set(settings.canopyPricingPlan || 'pay_as_you_go');
    this.dataForSeoLogin.set(settings.dataForSeoLogin);
    this.dataForSeoPassword.set(settings.dataForSeoPassword);
    this.defaultProvider.set(settings.defaultProvider);
    this.defaultModel.set(settings.defaultModel);
    this.serviceStatuses.set(settings.serviceStatuses ?? []);
  }
}
