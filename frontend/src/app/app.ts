import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthService } from './core/auth/auth.service';
import { ClerkTokenWire } from './core/auth/clerk-token-wire';
import { ThemeService } from './core/themes/theme.service';
import { AuthPage } from './pages/auth/auth-page';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AuthPage, ClerkTokenWire],
  template: `
    @if (auth.enteredApp()) {
      <router-outlet />
    } @else {
      <app-auth-page />
    }
    @if (auth.clerkEnabled()) {
      <app-clerk-token-wire />
    }
  `,
})
export class App implements OnInit {
  protected readonly auth = inject(AuthService);

  constructor() {
    inject(ThemeService);
  }

  ngOnInit(): void {
    void this.auth.restoreSession();
  }
}
