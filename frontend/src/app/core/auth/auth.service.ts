import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthApiService } from './auth-api.service';
import { AuthSession, AuthUser } from './auth.models';
import { AuthTokenService } from './auth-token.service';
import { AUTH_BOOT_CONFIG } from './auth.tokens';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = inject(AuthApiService);
  private readonly authTokenService = inject(AuthTokenService);
  private readonly bootConfig = inject(AUTH_BOOT_CONFIG);

  private clerkSignOut: (() => Promise<void>) | null = null;

  readonly clerkEnabled = signal(this.bootConfig.clerkEnabled);
  readonly me = signal<AuthUser | null>(null);
  readonly enteredApp = signal(false);
  readonly restoringSession = signal(false);
  readonly sessionError = signal('');

  readonly breakGlass = computed(() => this.me()?.breakGlass === true);
  readonly isAdmin = computed(() => this.me()?.isAdmin === true);

  constructor() {
    this.authTokenService.registerAuthRequiredHandler(() => {
      this.authTokenService.setOperatorBypassToken('');
      this.me.set(null);
      this.enteredApp.set(false);
    });
  }

  registerClerkSignOut(signOut: () => Promise<void>): void {
    this.clerkSignOut = signOut;
  }

  wireClerkGetToken(getToken: () => Promise<string | null>): void {
    this.authTokenService.setTokenProvider(async () => {
      if (this.authTokenService.getOperatorBypassToken()) {
        return null;
      }

      try {
        return await getToken();
      } catch {
        return null;
      }
    });
  }

  async restoreSession(): Promise<void> {
    this.restoringSession.set(true);
    try {
      await this.refreshMe();
    } finally {
      this.restoringSession.set(false);
    }
  }

  async refreshMe(): Promise<boolean> {
    this.sessionError.set('');

    const bypass = this.authTokenService.getOperatorBypassToken();
    const headers = await this.authTokenService.buildAuthHeaders();
    const hasAuth = Boolean(bypass) || headers.has('Authorization');

    if (!hasAuth) {
      this.me.set(null);
      this.enteredApp.set(false);
      return false;
    }

    try {
      const data = await firstValueFrom(this.authApi.getSession());
      return this.applySession(data, bypass);
    } catch {
      this.sessionError.set('Could not reach the server.');
      this.me.set(null);
      this.enteredApp.set(false);
      return false;
    }
  }

  async applyOperatorBypass(token: string): Promise<boolean> {
    this.authTokenService.setOperatorBypassToken(token.trim());
    return this.refreshMe();
  }

  clearOperatorBypass(): void {
    this.authTokenService.setOperatorBypassToken('');
    this.me.set(null);
    this.enteredApp.set(false);
  }

  async signOut(): Promise<void> {
    this.sessionError.set('');

    if (this.breakGlass()) {
      this.authTokenService.setOperatorBypassToken('');
    } else if (this.clerkSignOut) {
      await this.clerkSignOut();
    }

    this.me.set(null);
    this.enteredApp.set(false);
  }

  private applySession(data: AuthSession, bypass: string): boolean {
    if (!data.authenticated) {
      if (bypass) {
        this.authTokenService.setOperatorBypassToken('');
      }

      this.sessionError.set(
        data.reason?.trim() ||
          'Sign-in could not be verified. Check CLERK_JWT_ISSUER and CLERK_PUBLISHABLE_KEY in .env or deployment settings.',
      );
      this.me.set(null);
      this.enteredApp.set(false);
      return false;
    }

    this.me.set({
      id: data.id ?? 0,
      email: data.email ?? '',
      firstName: data.firstName ?? '',
      lastName: data.lastName ?? '',
      isAdmin: Boolean(data.isAdmin),
      breakGlass: Boolean(data.breakGlass),
    });
    this.enteredApp.set(true);
    this.sessionError.set('');
    return true;
  }
}

export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

export async function resolveClerkSessionToken(
  getToken: (options?: { skipCache?: boolean }) => Promise<string | null>,
): Promise<string | null> {
  try {
    const fresh = await getToken({ skipCache: true });
    if (fresh) {
      return fresh;
    }

    return (await getToken()) ?? null;
  } catch {
    return null;
  }
}
