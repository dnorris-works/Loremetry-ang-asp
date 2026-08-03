import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ClerkLoadedDirective,
  ClerkLoadingDirective,
  ClerkService,
  ClerkSignInComponent,
  ClerkSignedInDirective,
  ClerkSignedOutDirective,
} from 'ngx-clerk';

import { AuthService, sleep } from '../../core/auth/auth.service';

@Component({
  selector: 'app-auth-clerk-sign-in',
  imports: [
    ClerkSignInComponent,
    ClerkLoadingDirective,
    ClerkLoadedDirective,
    ClerkSignedInDirective,
    ClerkSignedOutDirective,
  ],
  templateUrl: './auth-clerk-sign-in.html',
  styleUrl: './auth-clerk-sign-in.css',
})
export class AuthClerkSignIn {
  protected readonly auth = inject(AuthService);
  private readonly clerk = inject(ClerkService, { optional: true });

  constructor() {
    effect(() => {
      if (this.clerk?.isSignedIn() && !this.auth.breakGlass()) {
        void this.retrySessionAfterSignIn();
      }
    });
  }

  private async retrySessionAfterSignIn(): Promise<void> {
    for (let attempt = 0; attempt < 10; attempt += 1) {
      if (await this.auth.refreshMe()) {
        return;
      }

      await sleep(350);
    }
  }
}

@Component({
  selector: 'app-auth-operator-form',
  imports: [FormsModule],
  templateUrl: './auth-operator-form.html',
  styleUrl: './auth-operator-form.css',
})
export class AuthOperatorForm {
  protected readonly auth = inject(AuthService);
  protected readonly operatorToken = signal('');
  protected readonly operatorError = signal('');
  protected readonly submitting = signal(false);

  protected async onSubmit(event: Event): Promise<void> {
    event.preventDefault();
    this.operatorError.set('');

    const token = this.operatorToken().trim();
    if (!token) {
      this.operatorError.set('Enter the operator bypass token.');
      return;
    }

    this.submitting.set(true);
    try {
      const ok = await this.auth.applyOperatorBypass(token);
      if (!ok) {
        this.operatorError.set('Invalid operator token.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}

@Component({
  selector: 'app-auth-page',
  imports: [AuthClerkSignIn, AuthOperatorForm],
  templateUrl: './auth-page.html',
  styleUrl: './auth-page.css',
})
export class AuthPage {
  protected readonly auth = inject(AuthService);
  private readonly clerk = inject(ClerkService, { optional: true });

  protected showOperator(): boolean {
    if (!this.auth.clerkEnabled()) {
      return true;
    }

    const clerkSignedIn = this.clerk?.isSignedIn() ?? false;
    return !clerkSignedIn || Boolean(this.auth.sessionError());
  }
}
