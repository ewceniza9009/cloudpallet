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
      {
        path: 'receiving',
        loadComponent: () =>
          import('./mobile-receiving/mobile-receiving.component').then(
            (c) => c.MobileReceivingComponent
          ),
      },
      {
        path: 'putaway',
        loadComponent: () =>
          import('./mobile-putaway/mobile-putaway.component').then(
            (c) => c.MobilePutawayComponent
          ),
      },
      {
        path: 'inventory',
        loadComponent: () =>
          import('./mobile-inventory/mobile-inventory.component').then(
            (c) => c.MobileInventoryComponent
          ),
      },
      {
        path: 'vas',
        loadComponent: () =>
          import('./mobile-vas/mobile-vas.component').then(
            (c) => c.MobileVASComponent
          ),
      },
      {
        path: 'shipping',
        loadComponent: () =>
          import('./mobile-shipping/mobile-shipping.component').then(
            (c) => c.MobileShippingComponent
          ),
      },
    ],
  },
];
