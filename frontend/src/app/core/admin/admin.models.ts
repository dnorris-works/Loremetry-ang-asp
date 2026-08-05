export interface SchemaInfo {
  name: string;
}

export interface SchemaObject {
  name: string;
  type: string;
}

export interface ColumnInfo {
  name: string;
  dataType: string;
  isNullable: boolean;
  defaultValue: string | null;
}

export interface SqlQueryResult {
  columns: string[];
  rows: unknown[][];
  rowsAffected: number | null;
  message: string | null;
}

export interface PlatformServiceStatus {
  service: string;
  label: string;
  state: string;
  message: string | null;
  testedAt: string | null;
  configured: boolean;
}

export interface PlatformSettings {
  anthropicApiKey: string;
  tokenmixApiKey: string;
  canopyApiKey: string;
  dataForSeoLogin: string;
  dataForSeoPassword: string;
  defaultProvider: string;
  defaultModel: string;
  canopyPricingPlan: string;
  anthropicConfigured: boolean;
  tokenmixConfigured: boolean;
  canopyConfigured: boolean;
  dataForSeoConfigured: boolean;
  serviceStatuses: PlatformServiceStatus[];
}

export interface UpdatePlatformSettingsRequest {
  anthropicApiKey: string;
  tokenmixApiKey: string;
  canopyApiKey: string;
  dataForSeoLogin: string;
  dataForSeoPassword: string;
  defaultProvider: string;
  defaultModel: string;
  canopyPricingPlan: string;
}

export interface CanopyPricingPlan {
  id: string;
  displayName: string;
  monthlyFeeUsd: number;
  monthlyRequestAllowance: number;
  overagePricePerRequest: number;
  sortOrder: number;
  syncedAt: string;
}

export interface CanopyPricingOverview {
  activePlanId: string;
  requestsUsedThisMonth: number;
  plans: CanopyPricingPlan[];
}

export interface CanopyCostEstimate {
  planId: string;
  planDisplayName: string;
  requestCount: number;
  requestsUsedThisMonth: number;
  billableRequests: number;
  monthlyFeeUsd: number;
  marginalCostUsd: number;
  hardLimitReached: boolean;
  pricingMissing: boolean;
}

export interface CanopyOperationEstimate {
  operationId: string;
  label: string;
  requestCount: number;
  estimate: CanopyCostEstimate;
}

export type TestPlatformSettingsRequest = UpdatePlatformSettingsRequest;

export interface PlatformServiceTestResult {
  service: string;
  label: string;
  configured: boolean;
  success: boolean;
  error: string | null;
}

export interface PlatformConnectionTestResult {
  results: PlatformServiceTestResult[];
}

export interface WinningCatCatalogStatus {
  success: boolean;
  hasData: boolean;
  ready: boolean;
  kindleCount: number;
  booksCount: number;
  totalCount: number;
  lastImportAt: string | null;
  message: string;
  error: string | null;
}

export interface WinningCatImportResult {
  success: boolean;
  imported: number;
  skippedOtherDepartment: number;
  skippedUnparseable: number;
  staleCount: number;
  importedAt: string;
  error: string | null;
}

export interface WinningCatStaleCleanupResult {
  success: boolean;
  removed: number;
  error: string | null;
}
