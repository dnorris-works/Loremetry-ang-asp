import { Component, input } from '@angular/core';

@Component({
  selector: 'app-footer',
  templateUrl: './app-footer.html',
  styleUrl: './app-footer.css',
})
export class AppFooter {
  readonly appName = input('Loremetry');
  readonly year = input(new Date().getFullYear());
}
