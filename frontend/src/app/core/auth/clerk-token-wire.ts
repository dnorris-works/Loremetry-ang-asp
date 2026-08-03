import { Component, effect, inject, OnInit } from '@angular/core';
import { ClerkService } from 'ngx-clerk';

import { AuthService, resolveClerkSessionToken, sleep } from '../../core/auth/auth.service';

@Component({
  selector: 'app-clerk-token-wire',
  template: '<span class="clerk-wire" aria-hidden="true"></span>',
  styles: `
    .clerk-wire {
      display: none;
    }
  `,
})
export class ClerkTokenWire implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly clerk = inject(ClerkService);

  constructor() {
    effect(() => {
      if (this.clerk.isSignedIn() && !this.auth.breakGlass()) {
        void this.completeClerkSignIn();
      }
    });
  }

  ngOnInit(): void {
    this.auth.wireClerkGetToken(() =>
      resolveClerkSessionToken((options) => this.clerk.getToken(options)),
    );

    this.auth.registerClerkSignOut(async () => {
      await this.clerk.signOut();
    });

    void this.completeClerkSignIn();
  }

  private async completeClerkSignIn(): Promise<void> {
    if (!this.clerk.isSignedIn() || this.auth.breakGlass()) {
      return;
    }

    for (let attempt = 0; attempt < 10; attempt += 1) {
      if (await this.auth.refreshMe()) {
        return;
      }

      await sleep(350);
    }
  }
}
