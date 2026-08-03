import { EnvironmentProviders, Provider } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideClerk } from 'ngx-clerk';

import { appConfig } from './app/app.config';
import { App } from './app/app';
import { AuthConfig } from './app/core/auth/auth.models';
import { AUTH_BOOT_CONFIG } from './app/core/auth/auth.tokens';
import { environment } from './environments/environment';

async function loadAuthConfig(): Promise<AuthConfig> {
  try {
    const response = await fetch(`${environment.apiBaseUrl}/api/auth/config`);
    const data = await response.json();
    return {
      clerkEnabled: Boolean(data.clerkEnabled),
      publishableKey: data.publishableKey ?? '',
    };
  } catch {
    return { clerkEnabled: false, publishableKey: '' };
  }
}

async function bootstrap(): Promise<void> {
  const authConfig = await loadAuthConfig();
  const providers: (Provider | EnvironmentProviders)[] = [
    ...appConfig.providers,
    { provide: AUTH_BOOT_CONFIG, useValue: authConfig },
  ];

  if (authConfig.clerkEnabled && authConfig.publishableKey) {
    providers.push(provideClerk({ publishableKey: authConfig.publishableKey }));
  }

  await bootstrapApplication(App, { providers });
}

void bootstrap().catch((error) => console.error(error));
