export const APP_SETTING_KEYS = {
  theme: 'theme',
} as const;

export type AppSettingKey = (typeof APP_SETTING_KEYS)[keyof typeof APP_SETTING_KEYS];

export interface AppSetting {
  key: string;
  value: string;
  updatedAt: string;
}
