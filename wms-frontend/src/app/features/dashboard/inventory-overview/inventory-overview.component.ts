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
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
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
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatListModule,
    MatDividerModule,
    MatInputModule,
    MatFormFieldModule,
    FormsModule,
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

  filters = signal({
    room: '',
    bay: '',
    row: '',
    column: '',
    level: ''
  });

  filteredData = computed(() => {
    const data = this.overviewData();
    const f = this.filters();
    
    // Normalize filters
    const roomFilter = f.room.toLowerCase().trim();
    const bayFilter = f.bay.toLowerCase().trim();
    const rowFilter = f.row.toLowerCase().trim();
    const colFilter = f.column.toLowerCase().trim();
    const levelFilter = f.level.toLowerCase().trim();

    const hasFilter = roomFilter || bayFilter || rowFilter || colFilter || levelFilter;

    if (!data || !hasFilter) return data;

    const filteredRooms = data.rooms
      .filter(room => !roomFilter || room.roomName.toLowerCase().includes(roomFilter))
      .map((room) => {
        const filteredBays = room.bays
          .filter(bay => !bayFilter || bay.bayName.toLowerCase().includes(bayFilter))
          .map((bay) => {
            // Filter locations based on Row, Column, Level
            const filteredLocations = bay.locations.filter((loc) => {
              const matchRow = !rowFilter || loc.row.toString().toLowerCase().includes(rowFilter);
              const matchCol = !colFilter || loc.column.toString().toLowerCase().includes(colFilter);
              const matchLevel = !levelFilter || loc.level.toString().toLowerCase().includes(levelFilter);
              
              return matchRow && matchCol && matchLevel;
            });

            if (filteredLocations.length > 0) {
              return { ...bay, locations: filteredLocations };
            }
            return null;
          })
          .filter((bay): bay is NonNullable<typeof bay> => bay !== null);

        if (filteredBays.length > 0) {
          return { ...room, bays: filteredBays };
        }
        return null;
      })
      .filter((room): room is NonNullable<typeof room> => room !== null);

    return { rooms: filteredRooms };
  });

  // Helper to check if any filter is active for auto-expansion
  hasActiveFilters = computed(() => {
    const f = this.filters();
    return !!(f.room || f.bay || f.row || f.column || f.level);
  });

  updateFilter(field: keyof ReturnType<typeof this.filters>, value: string) {
    this.filters.update(current => ({ ...current, [field]: value }));
  }

  clearFilters() {
    this.filters.set({
      room: '',
      bay: '',
      row: '',
      column: '',
      level: ''
    });
  }

  summaryData = computed<SummaryRow[]>(() => {
    const data = this.overviewData(); // Summary always shows full data
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
