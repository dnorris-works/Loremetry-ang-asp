import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/app-shell/app-shell').then((m) => m.AppShell),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/landing/landing').then((m) => m.Landing),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/settings/settings').then((m) => m.Settings),
      },
      {
        path: 'admin',
        loadComponent: () =>
          import('./pages/admin/admin-layout/admin-layout').then((m) => m.AdminLayout),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'schema' },
          {
            path: 'schema',
            loadComponent: () =>
              import('./pages/admin/admin-schema/admin-schema').then((m) => m.AdminSchema),
          },
          {
            path: 'console',
            loadComponent: () =>
              import('./pages/admin/admin-console/admin-console').then((m) => m.AdminConsole),
          },
        ],
      },
    ],
  },
];
