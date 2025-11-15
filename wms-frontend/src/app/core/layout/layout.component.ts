import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';
import { WarehouseStateService } from '../services/warehouse-state.service';
import { NotificationService } from '../services/notification.service';
import { SignalRService } from '../services/signal-r.service';

export interface WarehouseDto { id: string; name: string; }

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule, RouterModule, MatToolbarModule, MatIconModule,
    MatButtonModule, MatSidenavModule, MatListModule, MatDividerModule,
    MatFormFieldModule, MatSelectModule, MatTooltipModule, MatMenuModule,
    MatBadgeModule, MatInputModule
  ],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent implements OnInit, OnDestroy {
  public authService = inject(AuthService);
  public warehouseState = inject(WarehouseStateService);
  public notificationService = inject(NotificationService);
  private router = inject(Router);
  private http = inject(HttpClient);
  private signalRService = inject(SignalRService);

  isExpanded = signal(true);
  warehouses = signal<WarehouseDto[]>([]);
  currentUser = this.authService.currentUser;


  isOperationsExpanded = signal(true);

  ngOnInit(): void {
    this.loadWarehouses();

   
    this.signalRService.startConnections()
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

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  stopPropagation(event: Event): void {
    event.stopPropagation();
  }
}
