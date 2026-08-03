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
        path: 'admin',
        loadComponent: () =>
          import('./pages/admin/admin-layout/admin-layout').then((m) => m.AdminLayout),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'collections' },
          {
            path: 'collections',
            loadComponent: () =>
              import('./pages/admin/admin-collections/admin-collections').then(
                (m) => m.AdminCollections,
              ),
          },
          {
            path: 'collections/:id',
            loadComponent: () =>
              import('./pages/admin/admin-collection-detail/admin-collection-detail').then(
                (m) => m.AdminCollectionDetail,
              ),
          },
          {
            path: 'schema',
            loadComponent: () =>
              import('./pages/admin/admin-schema/admin-schema').then((m) => m.AdminSchema),
          },
        ],
      },
    ],
  },
];
