import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LayoutComponent } from './core/layout/layout.component';
import { AuthService } from './core/services/auth.service';
import { adminGuard } from './core/guards/admin.guard';

const financeGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const userRole = authService.currentUserRole();

  if (userRole === 'Admin' || userRole === 'Finance') {
    return true;
  }

  return router.parseUrl('/dashboard');
};

const landingGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  if (authService.isLoggedIn()) {
    return router.parseUrl('/dashboard');
  }
  return true;
};

export const routes: Routes = [
  {
    path: '',
    canActivate: [landingGuard],
    loadComponent: () =>
      import('./features/landing/landing.component').then(
        (c) => c.LandingComponent
      ),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then((c) => c.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/register/register.component').then(
        (c) => c.RegisterComponent
      ),
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import(
            './features/dashboard/energy-dashboard/energy-dashboard.component'
          ).then((c) => c.EnergyDashboardComponent),
      },
      {
        path: 'inventory-overview',
        loadComponent: () =>
          import(
            './features/dashboard/inventory-overview/inventory-overview.component'
          ).then((c) => c.InventoryOverviewComponent),
      },
      {
        path: 'docks',
        loadComponent: () =>
          import(
            './features/dock-scheduling/dock-scheduler/dock-scheduler.component'
          ).then((c) => c.DockSchedulerComponent),
      },
      {
        path: 'dock-monitoring',
        loadComponent: () =>
          import('./features/dock-monitoring/dock-monitoring.component').then(
            (c) => c.DockMonitoringComponent
          ),
      },
      {
        path: 'yard',
        loadComponent: () =>
          import('./features/yard/yard-management.component').then(
            (c) => c.YardManagementComponent
          ),
      },
      {
        path: 'receiving',
        loadComponent: () =>
          import('./features/receiving/receiving.component').then(
            (c) => c.ReceivingComponent
          ),
      },
      {
        path: 'receiving/:id',
        loadComponent: () =>
          import('./features/receiving/receiving-session.component').then(
            (c) => c.ReceivingSessionComponent
          ),
      },
      {
        path: 'receiving/new',
        loadComponent: () =>
          import('./features/receiving/receiving-session.component').then(
            (c) => c.ReceivingSessionComponent
          ),
      },
      {
        path: 'putaway',
        loadComponent: () =>
          import('./features/putaway/putaway-list.component').then(
            (c) => c.PutawayListComponent
          ),
      },

      {
        path: 'inventory-manager',
        loadComponent: () =>
          import(
            './features/inventory/inventory-manager/inventory-manager.component'
          ).then((c) => c.InventoryManagerComponent),
      },

      {
        path: 'repacking',
        loadComponent: () =>
          import('./features/inventory/repack/repack.component').then(
            (c) => c.RepackComponent
          ),
      },
      {
        path: 'kitting',
        loadComponent: () =>
          import('./features/inventory/kitting/kitting.component').then(
            (c) => c.KittingComponent
          ),
      },
      {
        path: 'cycle-counting',
        loadComponent: () =>
          import(
            './features/inventory/cycle-counting/cycle-counting.component'
          ).then((c) => c.CycleCountingComponent),
      },
      {
        path: 'vas-transactions',
        loadComponent: () =>
          import(
            './features/inventory/vas-transactions-list/vas-transactions-list.component'
          ).then((c) => c.VasTransactionsListComponent),
      },
      {
        path: 'tracer',
        loadComponent: () =>
          import(
            './features/tracer/pallet-tracer/pallet-tracer.component'
          ).then((c) => c.PalletTracerComponent),
      },
      {
        path: 'picking',
        loadComponent: () =>
          import('./features/picking/picking.component').then(
            (c) => c.PickingComponent
          ),
      },
      {
        path: 'shipping',
        loadComponent: () =>
          import('./features/shipping/shipping.component').then(
            (c) => c.ShippingComponent
          ),
      },
      {
        path: 'billing',
        canActivate: [financeGuard],
        loadComponent: () =>
          import('./features/billing/invoice-list/invoice-list.component').then(
            (c) => c.InvoiceListComponent
          ),
      },
      {
        path: 'billing/:id',
        canActivate: [financeGuard],
        loadComponent: () =>
          import(
            './features/billing/invoice-detail/invoice-detail.component'
          ).then((c) => c.InvoiceDetailComponent),
      },
      {
        path: 'admin/company',
        canActivate: [adminGuard],
        loadComponent: () =>
          import(
            './features/admin/company-management/company-management.component'
          ).then((c) => c.CompanyManagementComponent),
      },
      {
        path: 'setup/warehouses',
        canActivate: [adminGuard],
        loadComponent: () =>
          import(
            './features/admin/warehouse-management/warehouse-management.component'
          ).then((c) => c.WarehouseManagementComponent),
        children: [
          {
            path: 'detail/:id',
            loadComponent: () =>
              import(
                './features/admin/warehouse-management/warehouse-detail/warehouse-detail.component'
              ).then((c) => c.WarehouseDetailComponent),
          },
          {
            path: 'new',
            loadComponent: () =>
              import(
                './features/admin/warehouse-management/warehouse-detail/warehouse-detail.component'
              ).then((c) => c.WarehouseDetailComponent),
          },
          {
            path: '',
            loadComponent: () =>
              import(
                './features/admin/warehouse-management/warehouse-list/warehouse-list.component'
              ).then((c) => c.WarehouseListComponent),
          },
        ],
      },
      {
        path: 'setup/rooms',
        canActivate: [adminGuard],
        loadComponent: () =>
          import(
            './features/admin/location-management/location-management.component'
          ).then((c) => c.LocationManagementComponent),
        children: [
          {
            path: 'detail/:id',
            loadComponent: () =>
              import(
                './features/admin/location-management/room-detail/room-detail.component'
              ).then((c) => c.RoomDetailComponent),
          },
          {
            path: 'new',
            loadComponent: () =>
              import(
                './features/admin/location-management/room-detail/room-detail.component'
              ).then((c) => c.RoomDetailComponent),
          },
          {
            path: '',
            loadComponent: () =>
              import(
                './features/admin/location-management/room-list/room-list.component'
              ).then((c) => c.RoomListComponent),
          },
        ],
      },
      {
        path: 'setup/dock-yard',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/dock-yard-setup/dock-yard-setup.component')
          .then(c => c.DockYardSetupComponent)
      },
      {
        path: 'setup/suppliers',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/supplier-setup/supplier-setup.component')
          .then(c => c.SupplierSetupComponent)
      },
      {
        path: 'setup/accounts',
        canActivate: [adminGuard], // or financeGuard
        loadComponent: () => import('./features/admin/account-setup/account-setup.component')
          .then(c => c.AccountSetupComponent)
      },
      {
        path: 'setup/carriers',
        canActivate: [adminGuard],
        loadComponent: () =>
          import(
            './features/admin/carrier-management/carrier-management.component'
          ).then((c) => c.CarrierManagementComponent),
        children: [
          {
            path: 'detail/:id',
            loadComponent: () =>
              import(
                './features/admin/carrier-management/carrier-detail/carrier-detail.component'
              ).then((c) => c.CarrierDetailComponent),
          },
          {
            path: 'new',
            loadComponent: () =>
              import(
                './features/admin/carrier-management/carrier-detail/carrier-detail.component'
              ).then((c) => c.CarrierDetailComponent),
          },
          {
            path: '',
            loadComponent: () =>
              import(
                './features/admin/carrier-management/carrier-list/carrier-list.component'
              ).then((c) => c.CarrierListComponent),
          },
        ],
      },
      {
        path: 'setup/units-of-measure',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/uom-setup/uom-setup.component')
          .then(c => c.UomSetupComponent)
      },
      {
        path: 'setup/pallet-types',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/pallet-type-setup/pallet-type-setup.component')
          .then(c => c.PalletTypeSetupComponent)
      },
      {
        path: 'admin/users',
        canActivate: [adminGuard],
        loadComponent: () =>
          import(
            './features/admin/user-management/user-management.component'
          ).then((c) => c.UserManagementComponent),
      },
      {
        path: 'admin/rates',
        canActivate: [financeGuard],
        loadComponent: () =>
          import(
            './features/admin/rate-management/rate-management.component'
          ).then((c) => c.RateManagementComponent),
        children: [
          {
            path: 'detail/:id',
            loadComponent: () =>
              import(
                './features/admin/rate-management/rate-detail/rate-detail.component'
              ).then((c) => c.RateDetailComponent),
          },
          {
            path: 'new',
            loadComponent: () =>
              import(
                './features/admin/rate-management/rate-detail/rate-detail.component'
              ).then((c) => c.RateDetailComponent),
          },
          {
            path: '',
            loadComponent: () =>
              import(
                './features/admin/rate-management/rate-list/rate-list.component'
              ).then((c) => c.RateListComponent),
          },
        ],
      },
      {
        path: 'admin/materials',
        canActivate: [adminGuard],
        loadComponent: () =>
          import(
            './features/admin/material-management/material-management.component'
          ).then((c) => c.MaterialManagementComponent),
        children: [
          {
            path: 'detail/:id',
            loadComponent: () =>
              import(
                './features/admin/material-management/material-detail/material-detail.component'
              ).then((c) => c.MaterialDetailComponent),
          },
          {
            path: 'new',
            loadComponent: () =>
              import(
                './features/admin/material-management/material-detail/material-detail.component'
              ).then((c) => c.MaterialDetailComponent),
          },
          {
            path: '',
            loadComponent: () =>
              import(
                './features/admin/material-management/material-list/material-list.component'
              ).then((c) => c.MaterialListComponent),
          },
        ],
      },
      {
        path: 'reports/inventory-ledger',
        canActivate: [financeGuard],
        loadComponent: () =>
          import(
            './features/reports/inventory-ledger/inventory-ledger.component'
          ).then((c) => c.InventoryLedgerComponent),
      },
      {
        path: 'reports/stock-on-hand',
        canActivate: [financeGuard],
        loadComponent: () =>
          import(
            './features/reports/stock-on-hand/stock-on-hand.component'
          ).then((c) => c.StockOnHandComponent),
      },
      {
        path: 'reports/custom',
        canActivate: [financeGuard],
        loadComponent: () =>
          import(
            './features/reports/custom-report/custom-report.component'
          ).then((c) => c.CustomReportComponent),
      },
      {
        path: 'reports/cycle-count-variance',
        canActivate: [financeGuard],
        loadComponent: () =>
          import(
            './features/reports/cycle-count-variance/cycle-count-variance.component'
          ).then((c) => c.CycleCountVarianceComponent),
      },
      {
        path: 'reports/activity-log',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/reports/activity-log/activity-log.component').then(
            (c) => c.ActivityLogComponent
          ),
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: '', pathMatch: 'full' },
];
