import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnDestroy, OnInit, effect, inject, signal } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterModule } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';
import { SignalRService } from '../services/signal-r.service';
import { WarehouseStateService } from '../services/warehouse-state.service';

export interface WarehouseDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatSidenavModule,
    MatListModule,
    MatDividerModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTooltipModule,
    MatMenuModule,
    MatBadgeModule,
    MatInputModule,
  ],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss'],
})
export class LayoutComponent implements OnInit, OnDestroy {
  public authService = inject(AuthService);
  public warehouseState = inject(WarehouseStateService);
  public notificationService = inject(NotificationService);
  private router = inject(Router);
  private http = inject(HttpClient);
  private signalRService = inject(SignalRService);

  // Helper to initialize signals from localStorage
  private getSavedState(key: string, defaultValue: boolean): boolean {
    try {
      const saved = localStorage.getItem('sidebar_state');
      if (saved) {
        const state = JSON.parse(saved);
        return state[key] !== undefined ? state[key] : defaultValue;
      }
    } catch (e) {
      return defaultValue;
    }
    return defaultValue;
  }

  isExpanded = signal(this.getSavedState('isExpanded', true));
  warehouses = signal<WarehouseDto[]>([]);
  currentUser = this.authService.currentUser;

  // Sidebar Section Expansion States
  isOperationsExpanded = signal(this.getSavedState('isOperationsExpanded', true));
  isSetupExpanded = signal(this.getSavedState('isSetupExpanded', false));
  isAdminExpanded = signal(this.getSavedState('isAdminExpanded', false));
  isDashboardsExpanded = signal(this.getSavedState('isDashboardsExpanded', false));
  isReportsExpanded = signal(this.getSavedState('isReportsExpanded', true));

  constructor() {
    // Effect to save state whenever any signal changes
    effect(() => {
      const state = {
        isExpanded: this.isExpanded(),
        isOperationsExpanded: this.isOperationsExpanded(),
        isSetupExpanded: this.isSetupExpanded(),
        isAdminExpanded: this.isAdminExpanded(),
        isDashboardsExpanded: this.isDashboardsExpanded(),
        isReportsExpanded: this.isReportsExpanded(),
      };
      localStorage.setItem('sidebar_state', JSON.stringify(state));
    });
  }

  ngOnInit(): void {
    this.loadWarehouses();

    this.signalRService
      .startConnections()
      .then(() => console.log('✅ SignalR connections established globally from LayoutComponent.'))
      .catch(err => console.error('❌ Global SignalR connection failed:', err));
  }

  ngOnDestroy(): void {
    this.signalRService.stopConnections();
  }

  loadWarehouses(): void {
    this.http.get<WarehouseDto[]>(`${environment.apiUrl}/Lookups/warehouses`).subscribe(data => {
      this.warehouses.set(data);
      if (!this.warehouseState.selectedWarehouseId() && data.length > 0) {
        this.warehouseState.setSelectedWarehouseId(data[0].id);
      }
    });
  }

  getWarehouseName(warehouseId: string): string {
    const warehouse = this.warehouses().find(wh => wh.id === warehouseId);
    return warehouse?.name || 'Select Warehouse';
  }

  onWarehouseChange(warehouseId: string): void {
    this.warehouseState.setSelectedWarehouseId(warehouseId);
    window.location.reload();
  }

  toggleSidenav(): void {
    this.isExpanded.update(expanded => !expanded);
  }

  toggleOperationsMenu(): void {
    this.isOperationsExpanded.update(expanded => !expanded);
  }

  toggleSetupMenu(): void {
    this.isSetupExpanded.update(expanded => !expanded);
  }

  toggleAdminMenu(): void {
    this.isAdminExpanded.update(expanded => !expanded);
  }

  toggleDashboardsMenu(): void {
    this.isDashboardsExpanded.update(expanded => !expanded);
  }

  toggleReportsMenu(): void {
    this.isReportsExpanded.update(expanded => !expanded);
  }


  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  stopPropagation(event: Event): void {
    event.stopPropagation();
  }
}
