import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  effect,
  computed,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { Subscription } from 'rxjs';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  SignalRService,
  DockStatusUpdate,
} from '../../core/services/signal-r.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { WarehouseStateService } from '../../core/services/warehouse-state.service';

export interface DockStatusDto {
  id: string;
  name: string;
  isAvailable: boolean;
  currentAppointmentId: string | null;
  licensePlate?: string;
  carrierName?: string;
  arrival?: string;
}

@Component({
  selector: 'app-dock-monitoring',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatButtonToggleModule,
  ],
  templateUrl: './dock-monitoring.component.html',
  styleUrls: ['./dock-monitoring.component.scss'],
})
export class DockMonitoringComponent implements OnInit, OnDestroy {
  private http = inject(HttpClient);
  private signalR = inject(SignalRService);
  private snackBar = inject(MatSnackBar);
  private router = inject(Router);
  private warehouseState = inject(WarehouseStateService);
  private sub!: Subscription;
  private timerSub!: Subscription;

  docks = signal<DockStatusDto[]>([]);
  isLoading = signal(true);
  filter = signal<'all' | 'available' | 'occupied'>('all');
  now = signal(Date.now());

  // Computed Metrics
  totalDocks = computed(() => this.docks().length);
  availableDocks = computed(() => this.docks().filter(d => d.isAvailable).length);
  occupiedDocks = computed(() => this.docks().filter(d => !d.isAvailable).length);
  utilization = computed(() => {
    const total = this.totalDocks();
    return total > 0 ? Math.round((this.occupiedDocks() / total) * 100) : 0;
  });

  // Filtered List
  filteredDocks = computed(() => {
    const currentFilter = this.filter();
    const allDocks = this.docks();
    if (currentFilter === 'available') return allDocks.filter(d => d.isAvailable);
    if (currentFilter === 'occupied') return allDocks.filter(d => !d.isAvailable);
    return allDocks;
  });

  constructor() {
    effect(() => {
      this.warehouseState.selectedWarehouseId();
      this.loadInitialState();
    });
  }

  ngOnInit(): void {
    this.sub = this.signalR.dockStatusUpdate$.subscribe((update) =>
      this.handleUpdate(update)
    );
    
    // Update 'now' signal every minute to refresh duration calculations
    // Using interval from rxjs would be cleaner but let's stick to standard timer for now or just setInterval
    // Actually, let's use a simple interval
    this.timerSub = new Subscription();
    const intervalId = setInterval(() => this.now.set(Date.now()), 60000);
    this.timerSub.add(() => clearInterval(intervalId));
  }

  loadInitialState(): void {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      this.docks.set([]);
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    const params = new HttpParams().set('warehouseId', warehouseId);
    this.http
      .get<DockStatusDto[]>(`${environment.apiUrl}/Lookups/docks`, { params })
      .subscribe({
        next: (data) => {
          this.docks.set(data);
          this.isLoading.set(false);
        },
        error: () => {
          this.snackBar.open('Failed to load dock status.', 'Close');
          this.isLoading.set(false);
        },
      });
  }

  handleUpdate(update: DockStatusUpdate): void {
    this.loadInitialState();
  }

  setFilter(filter: 'all' | 'available' | 'occupied'): void {
    this.filter.set(filter);
  }

  getDuration(arrivalDate: string | undefined): string {
    if (!arrivalDate) return '';
    const start = new Date(arrivalDate).getTime();
    const diff = this.now() - start;
    
    if (diff < 0) return 'Just now';

    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    }
    return `${minutes}m`;
  }

  startReceiving(dock: DockStatusDto): void {
    if (!dock.currentAppointmentId) {
      this.snackBar.open(
        'Error: No appointment associated with this dock.',
        'Close'
      );
      return;
    }
    this.router.navigate(['/receiving/new'], {
      state: { appointmentId: dock.currentAppointmentId },
    });
  }

  vacateDock(dockId: string): void {
    this.http
      .post(`${environment.apiUrl}/DockAppointments/${dockId}/vacate`, {})
      .subscribe({
        next: () =>
          this.snackBar.open(`Dock is now being vacated.`, 'OK', {
            duration: 3000,
          }),
        error: (err: any) =>
          this.snackBar.open('Failed to vacate dock.', 'Close'),
      });
  }

  ngOnDestroy(): void {
    if (this.sub) {
      this.sub.unsubscribe();
    }
    if (this.timerSub) {
      this.timerSub.unsubscribe();
    }
  }
}
