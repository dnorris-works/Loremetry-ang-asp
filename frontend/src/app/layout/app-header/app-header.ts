import { Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

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
  readonly appName = input('Loremetry');
  readonly navItems = input<NavItem[]>([
    { label: 'Home', route: '/' },
    { label: 'About', route: '/about' },
  ]);
}
