import { Injectable, signal } from '@angular/core';

const BYPASS_STORAGE_KEY = 'loremetry_admin_bypass';

@Injectable({ providedIn: 'root' })
export class AuthTokenService {
  private tokenProvider: (() => Promise<string | null>) | null = null;
  private onAuthRequired: (() => void) | null = null;

  registerAuthRequiredHandler(handler: () => void): void {
    this.onAuthRequired = handler;
  }

  setTokenProvider(provider: () => Promise<string | null>): void {
    this.tokenProvider = provider;
  }

  getOperatorBypassToken(): string {
    try {
      return sessionStorage.getItem(BYPASS_STORAGE_KEY)?.trim() ?? '';
    } catch {
      return '';
    }
  }

  setOperatorBypassToken(token: string): void {
    try {
      const trimmed = token.trim();
      if (trimmed) {
        sessionStorage.setItem(BYPASS_STORAGE_KEY, trimmed);
      } else {
        sessionStorage.removeItem(BYPASS_STORAGE_KEY);
      }
    } catch {
      /* ignore */
    }
  }

  async buildAuthHeaders(extra?: HeadersInit): Promise<Headers> {
    const headers = new Headers(extra);
    const bypass = this.getOperatorBypassToken();

    if (bypass) {
      headers.set('X-Loremetry-Admin-Bypass', bypass);
    }

    if (this.tokenProvider && !bypass) {
      const token = await this.tokenProvider();
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
    }

    return headers;
  }

  notifyAuthRequired(): void {
    this.onAuthRequired?.();
  }

  isAuthFailureMessage(message: string): boolean {
    const normalized = message.toLowerCase();
    return (
      normalized.includes('clerk is not configured') ||
      normalized.includes('missing authorization') ||
      normalized.includes('jwt invalid') ||
      normalized.includes('operator access required')
    );
  }
}
