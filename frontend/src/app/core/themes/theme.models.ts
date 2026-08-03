export type ThemeId =
  | 'light'
  | 'dark'
  | 'system'
  | 'slate'
  | 'ocean'
  | 'forest'
  | 'rose'
  | 'sunset'
  | 'high-contrast';

export interface AppTheme {
  id: ThemeId;
  name: string;
  description: string;
  colorScheme: 'light' | 'dark';
  swatch: {
    background: string;
    surface: string;
    primary: string;
  };
}

export const APP_THEMES: readonly AppTheme[] = [
  {
    id: 'light',
    name: 'Light',
    description: 'Clean default light interface',
    colorScheme: 'light',
    swatch: {
      background: 'oklch(97% 0.005 260)',
      surface: 'oklch(100% 0 0)',
      primary: 'oklch(52% 0.19 264)',
    },
  },
  {
    id: 'dark',
    name: 'Dark',
    description: 'Standard dark mode',
    colorScheme: 'dark',
    swatch: {
      background: 'oklch(18% 0.02 260)',
      surface: 'oklch(22% 0.02 260)',
      primary: 'oklch(68% 0.16 264)',
    },
  },
  {
    id: 'system',
    name: 'System',
    description: 'Match your device light or dark preference',
    colorScheme: 'light',
    swatch: {
      background: 'oklch(90% 0.01 260)',
      surface: 'oklch(96% 0.005 260)',
      primary: 'oklch(52% 0.19 264)',
    },
  },
  {
    id: 'slate',
    name: 'Slate',
    description: 'Neutral gray dashboard theme',
    colorScheme: 'dark',
    swatch: {
      background: 'oklch(20% 0.01 250)',
      surface: 'oklch(26% 0.012 250)',
      primary: 'oklch(72% 0.04 250)',
    },
  },
  {
    id: 'ocean',
    name: 'Ocean',
    description: 'Cool blue web theme',
    colorScheme: 'dark',
    swatch: {
      background: 'oklch(22% 0.04 240)',
      surface: 'oklch(28% 0.05 240)',
      primary: 'oklch(72% 0.14 220)',
    },
  },
  {
    id: 'forest',
    name: 'Forest',
    description: 'Natural green theme',
    colorScheme: 'dark',
    swatch: {
      background: 'oklch(22% 0.03 155)',
      surface: 'oklch(28% 0.04 155)',
      primary: 'oklch(72% 0.14 155)',
    },
  },
  {
    id: 'rose',
    name: 'Rose',
    description: 'Soft rose light theme',
    colorScheme: 'light',
    swatch: {
      background: 'oklch(97% 0.02 350)',
      surface: 'oklch(100% 0.01 350)',
      primary: 'oklch(55% 0.2 350)',
    },
  },
  {
    id: 'sunset',
    name: 'Sunset',
    description: 'Warm amber evening theme',
    colorScheme: 'dark',
    swatch: {
      background: 'oklch(22% 0.04 55)',
      surface: 'oklch(28% 0.05 55)',
      primary: 'oklch(75% 0.16 65)',
    },
  },
  {
    id: 'high-contrast',
    name: 'High Contrast',
    description: 'Maximum readability and contrast',
    colorScheme: 'light',
    swatch: {
      background: 'oklch(100% 0 0)',
      surface: 'oklch(100% 0 0)',
      primary: 'oklch(45% 0.25 264)',
    },
  },
] as const;