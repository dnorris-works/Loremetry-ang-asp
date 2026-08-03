export interface AuthConfig {
  clerkEnabled: boolean;
  publishableKey: string;
}

export interface AuthUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  isAdmin: boolean;
  breakGlass: boolean;
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
