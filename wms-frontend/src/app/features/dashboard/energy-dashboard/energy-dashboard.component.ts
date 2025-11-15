import { Component, OnDestroy, OnInit, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxChartsModule, Color, ScaleType } from '@swimlane/ngx-charts';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import {
  SignalRService,
  TemperatureAlert,
} from '../../../core/services/signal-r.service';
import { Subscription } from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { HttpClient, HttpParams } from '@angular/common/http';
import { WarehouseStateService } from '../../../core/services/warehouse-state.service';

import { environment } from '../../../../environments/environment';

interface RoomDto {
  roomId: string;
  roomName: string;
  targetTemperature: number;
}
interface ChartData {
  name: string;
  value: number;
}
@Component({
  selector: 'app-energy-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    NgxChartsModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './energy-dashboard.component.html',
  styleUrls: ['./energy-dashboard.component.scss'],
})
export class EnergyDashboardComponent implements OnInit, OnDestroy {
  private signalRService = inject(SignalRService);
  private http = inject(HttpClient);
  private warehouseState = inject(WarehouseStateService);

  private sub!: Subscription;
  private apiUrl = environment.apiUrl;
  roomTemperatures: ChartData[] = [];
  isLoading = true;
  lastUpdateTime: Date | null = null;
  updateCount = 0;
  view: [number, number] = [800, 400];
  colorScheme: Color = {
    name: 'temperature',
    selectable: true,
    group: ScaleType.Ordinal,
    domain: ['#00bcd4', '#67e8f9', '#22d3ee', '#06b6d4', '#0891b2', '#0e7490'],
  };
  xAxis = true;
  yAxis = true;
  showXAxisLabel = true;
  showYAxisLabel = true;
  showDataLabel = true;
  animations = true;

  constructor() {
    effect(() => {
      this.warehouseState.selectedWarehouseId();
      this.initializeDashboard();
    });
  }

  ngOnInit(): void {
    this.setupResponsiveChart();
  }

  initializeDashboard(): void {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      this.isLoading = false;
      this.roomTemperatures = [];
      return;
    }
    this.isLoading = true;
    this.updateCount = 0;

    const params = new HttpParams().set('warehouseId', warehouseId);

    this.http
      .get<RoomDto[]>(`${this.apiUrl}/Warehouse/rooms`, { params })
      .subscribe({
        next: (rooms) => {
          this.roomTemperatures = rooms.map((room) => ({
            name: room.roomName,
            value: room.targetTemperature,
          }));
          this.isLoading = false;
          this.lastUpdateTime = new Date();

          if (this.sub) {
            this.sub.unsubscribe();
          }

          this.sub = this.signalRService.temperatureAlerts$.subscribe(
            (alert) => {
              this.handleTemperatureUpdate(alert);
            }
          );
        },
        error: (err: any) => {
          console.error('Failed to load initial room data', err);
          this.isLoading = false;
        },
      });
  }

  private handleTemperatureUpdate(alert: TemperatureAlert): void {
    const selectedWarehouseId = this.warehouseState.selectedWarehouseId();
    if (alert.warehouseId !== selectedWarehouseId) {
      return;
    }

    const roomIndex = this.roomTemperatures.findIndex(
      (room) => room.name === alert.roomName
    );

    if (roomIndex > -1) {
      this.roomTemperatures[roomIndex].value = alert.currentTemperature;
      this.roomTemperatures = [...this.roomTemperatures];
    } else {
      const newRoom: ChartData = {
        name: alert.roomName,
        value: alert.currentTemperature,
      };
      this.roomTemperatures = [...this.roomTemperatures, newRoom];
    }
    this.updateCount++;
    this.lastUpdateTime = new Date();
  }

  calculateAverageTemp(): number {
    if (this.roomTemperatures.length === 0) return 0;
    const sum = this.roomTemperatures.reduce(
      (acc, room) => acc + room.value,
      0
    );
    return sum / this.roomTemperatures.length;
  }

  calculateMinTemp(): number {
    if (this.roomTemperatures.length === 0) return 0;
    return Math.min(...this.roomTemperatures.map((room) => room.value));
  }

  calculateMaxTemp(): number {
    if (this.roomTemperatures.length === 0) return 0;
    return Math.max(...this.roomTemperatures.map((room) => room.value));
  }

  getLastUpdateTime(): string {
    return this.lastUpdateTime
      ? this.lastUpdateTime.toLocaleTimeString()
      : 'Never';
  }

  onSelect(event: any): void {
    console.log('Chart item selected:', event);
  }

  private setupResponsiveChart(): void {
    this.updateChartSize();
    window.addEventListener('resize', () => this.updateChartSize());
  }

  private updateChartSize(): void {
    const container = document.querySelector('.chart-container');
    if (container) {
      const width = Math.max(container.clientWidth - 40, 300);
      this.view = [width, 400];
    }
  }

  ngOnDestroy(): void {
    if (this.sub) {
      this.sub.unsubscribe();
    }
    window.removeEventListener('resize', () => this.updateChartSize());
  }
}
