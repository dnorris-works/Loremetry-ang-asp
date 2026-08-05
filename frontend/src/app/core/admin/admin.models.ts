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
