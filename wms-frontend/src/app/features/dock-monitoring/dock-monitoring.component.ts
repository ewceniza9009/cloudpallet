import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  effect,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
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

  docks = signal<DockStatusDto[]>([]);
  isLoading = signal(true);

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
  }
}
