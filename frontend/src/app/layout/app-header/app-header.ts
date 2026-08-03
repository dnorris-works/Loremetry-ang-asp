import { Component, computed, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

export interface NavItem {
  label: string;
  route: string;
}

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './app-header.html',
  styleUrl: './app-header.css',
})
export class AppHeader {
  protected readonly auth = inject(AuthService);

  readonly appName = input('Loremetry');
  readonly navItems = input<NavItem[]>([
    { label: 'Home', route: '/' },
  ]);

  protected readonly userLabel = computed(() => {
    const user = this.auth.me();
    if (!user) {
      return '';
    }

    const name = [user.firstName, user.lastName].filter(Boolean).join(' ').trim();
    return name || user.email;
  });

  protected onSignOut(): void {
    void this.auth.signOut();
  }
}
