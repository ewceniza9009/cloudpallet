import { Routes } from '@angular/router';

export const MOBILE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./mobile-layout/mobile-layout.component').then(
        (c) => c.MobileLayoutComponent
      ),
    children: [
      {
        path: '',
        redirectTo: 'menu',
        pathMatch: 'full',
      },
      {
        path: 'menu',
        loadComponent: () =>
          import('./mobile-menu/mobile-menu.component').then(
            (c) => c.MobileMenuComponent
          ),
      },
      {
        path: 'picking',
        loadComponent: () =>
          import('./mobile-picking/mobile-picking.component').then(
            (c) => c.MobilePickingComponent
          ),
      },
    ],
  },
];
