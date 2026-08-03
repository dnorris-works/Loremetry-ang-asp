import { InjectionToken } from '@angular/core';

import { AuthConfig } from './auth.models';

export const AUTH_BOOT_CONFIG = new InjectionToken<AuthConfig>('AUTH_BOOT_CONFIG', {
  factory: () => ({ clerkEnabled: false, publishableKey: '' }),
});
