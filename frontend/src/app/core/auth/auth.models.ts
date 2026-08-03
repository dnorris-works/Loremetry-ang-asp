export interface AuthConfig {
  clerkEnabled: boolean;
  publishableKey: string;
}

export interface AuthSession {
  authenticated: boolean;
  id?: number;
  email?: string;
  firstName?: string;
  lastName?: string;
  isAdmin?: boolean;
  breakGlass?: boolean;
  reason?: string;
}

export interface MeResponse {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  isAdmin: boolean;
  breakGlass: boolean;
}
