import {
  Component,
  OnInit,
  inject,
  signal,
  effect,
  computed,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { WarehouseStateService } from '../../../core/services/warehouse-state.service';
import {
  LocationOverviewDto,
  WarehouseApiService,
} from '../../warehouse/warehouse-api.service';
import { MatDialog } from '@angular/material/dialog';
import { LocationDetailDialogComponent } from '../../inventory/location-detail-dialog/location-detail-dialog.component';
import { LocationDto } from '../../warehouse/warehouse-api.service';

interface SummaryRow {
  room: string;
  totalBays: number;
  avgUtilization: number;
  overCapacityBays: number;
  fullBays: number;
}

@Component({
  selector: 'app-inventory-overview',
  standalone: true,
  imports: [
    CommonModule,
    DecimalPipe,
    MatCardModule,
    MatTableModule,
    MatExpansionModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatListModule,
    MatDividerModule,
  ],
  templateUrl: './inventory-overview.component.html',
  styleUrls: ['./inventory-overview.component.scss'],
})
export class InventoryOverviewComponent implements OnInit {
  private warehouseApi = inject(WarehouseApiService);
  private warehouseState = inject(WarehouseStateService);
  private dialog = inject(MatDialog);

  isLoading = signal(true);
  overviewData = signal<LocationOverviewDto | null>(null);
  summaryDisplayedColumns = [
    'room',
    'totalBays',
    'avgUtilization',
    'fullBays',
    'overCapacityBays',
  ];

  summaryData = computed<SummaryRow[]>(() => {
    const data = this.overviewData();
    if (!data) return [];
    return data.rooms.map((room) => ({
      room: room.roomName,
      totalBays: room.bays.length,
      avgUtilization: room.utilization,
      fullBays: room.bays.filter((b) => b.status === 'Full').length,
      overCapacityBays: room.bays.filter((b) => b.status === 'Over').length,
    }));
  });

  constructor() {
    effect(() => {
      const warehouseId = this.warehouseState.selectedWarehouseId();
      if (warehouseId) {
        this.loadOverview(warehouseId);
      }
    });
  }

  ngOnInit(): void {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (warehouseId) {
      this.loadOverview(warehouseId);
    }
  }

  loadOverview(warehouseId: string): void {
    this.isLoading.set(true);
    this.warehouseApi.getLocationOverview(warehouseId).subscribe({
      next: (data) => {
        this.overviewData.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.overviewData.set(null);
      },
    });
  }

  getLocationStatusClass(status: string, utilization: number): string {
    if (status === 'Over') return 'status-over';
    if (utilization === 0) return 'status-empty';
    if (utilization <= 60) return 'status-partial';
    if (utilization <= 90) return 'status-approaching';
    return 'status-full';
  }

  openLocationDialog(location: LocationDto) {
    this.dialog.open(LocationDetailDialogComponent, {
      data: location,
      width: '600px',
      maxWidth: '90vw',
      panelClass: 'location-detail-dialog-container'
    });
  }
}
