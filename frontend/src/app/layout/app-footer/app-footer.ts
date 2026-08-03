import { Component, inject, input } from '@angular/core';

import { DatabaseStatusService } from '../../core/database/database-status.service';

@Component({
  selector: 'app-footer',
  templateUrl: './app-footer.html',
  styleUrl: './app-footer.css',
})
export class AppFooter {
  private readonly databaseStatus = inject(DatabaseStatusService);

  readonly appName = input('Loremetry');
  readonly year = input(new Date().getFullYear());

  protected readonly dbStatus = this.databaseStatus.connectionStatus;
  protected readonly dbStatusLabel = this.databaseStatus.statusLabel;
}
