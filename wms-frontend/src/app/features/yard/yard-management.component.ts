import { Component, OnInit, inject, signal, computed, effect } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { forkJoin, filter } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import {
  YardApiService,
  YardAppointmentDto,
  OccupiedYardSpotDto,
} from './yard-api.service';
import { SelectYardSpotDialogComponent } from './select-yard-spot-dialog/select-yard-spot-dialog.component';
import { AddManifestDialogComponent } from './add-manifest-dialog/add-manifest-dialog.component';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';

interface DisplayableYardAppointmentDto extends YardAppointmentDto {
  isCheckedIn: boolean;
}

@Component({
  selector: 'app-yard-management',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatCardModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    ReactiveFormsModule,
    MatInputModule,
    MatFormFieldModule,
    MatDatepickerModule,
    MatNativeDateModule
  ],
  templateUrl: './yard-management.component.html',
  styleUrls: ['./yard-management.component.scss'],
})
export class YardManagementComponent implements OnInit {
  private yardApi = inject(YardApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  // Data Signals
  pendingAppointments = signal<DisplayableYardAppointmentDto[]>([]);
  occupiedSpots = signal<OccupiedYardSpotDto[]>([]);
  isLoading = signal(true);
  selectedDate = signal<Date>(new Date());

  // Search Controls & Signals
  arrivalsSearchControl = new FormControl('');
  yardSearchControl = new FormControl('');
  
  arrivalsSearchQuery = toSignal(this.arrivalsSearchControl.valueChanges, { initialValue: '' });
  yardSearchQuery = toSignal(this.yardSearchControl.valueChanges, { initialValue: '' });

  // Computed Filtered Lists
  filteredPendingAppointments = computed(() => {
    const appointments = this.pendingAppointments();
    const query = (this.arrivalsSearchQuery() || '').toLowerCase();

    if (!query) return appointments;

    return appointments.filter(
      (apt) =>
        apt.licensePlate.toLowerCase().includes(query) ||
        apt.carrierName.toLowerCase().includes(query) ||
        apt.dockName.toLowerCase().includes(query)
    );
  });

  filteredOccupiedSpots = computed(() => {
    const spots = this.occupiedSpots();
    const query = (this.yardSearchQuery() || '').toLowerCase();

    if (!query) return spots;

    return spots.filter(
      (spot) =>
        spot.licensePlate.toLowerCase().includes(query) ||
        spot.spotNumber.toLowerCase().includes(query)
    );
  });

  constructor() {
    // Reload data when date changes
    effect(() => {
      const date = this.selectedDate();
      this.loadYardData(date);
    }, { allowSignalWrites: true });
  }

  ngOnInit(): void {
    // Initial load handled by effect
  }

  onDateChange(event: any): void {
    if (event.value) {
      this.selectedDate.set(event.value);
    }
  }

  loadYardData(date: Date): void {
    this.isLoading.set(true);
    
    // Calculate start and end of the selected day in UTC
    const startOfDay = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0));
    const endOfDay = new Date(startOfDay);
    endOfDay.setUTCDate(startOfDay.getUTCDate() + 1);

    forkJoin({
      appointments: this.yardApi.getTodaysAppointments(
        startOfDay.toISOString(),
        endOfDay.toISOString()
      ),
      occupied: this.yardApi.getOccupiedYardSpots(),
    }).subscribe({
      next: ({ appointments, occupied }) => {
        const occupiedTruckIds = new Set(occupied.map((spot) => spot.truckId));
        const displayAppointments = appointments.map((apt) => ({
          ...apt,
          isCheckedIn: occupiedTruckIds.has(apt.truckId) || apt.status === 'InProgress',
        }));

        this.pendingAppointments.set(displayAppointments);
        this.occupiedSpots.set(occupied);
        this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load yard data.', 'Close', {
          duration: 5000,
        });
        this.isLoading.set(false);
      },
    });
  }

  checkIn(appointment: DisplayableYardAppointmentDto): void {
    if (appointment.isCheckedIn) {
      this.snackBar.open(
        `Truck ${appointment.licensePlate} is already checked in.`,
        'OK',
        { duration: 3000 }
      );
      return;
    }

    const dialogRef = this.dialog.open(SelectYardSpotDialogComponent);

    dialogRef
      .afterClosed()
      .pipe(filter((result): result is string => !!result))
      .subscribe((selectedSpotId: string) => {
        this.yardApi
          .checkInTruck(appointment.truckId, selectedSpotId)
          .subscribe({
            next: () => {
              this.snackBar.open(`Truck checked in successfully!`, 'OK', {
                duration: 5000,
              });
              this.loadYardData(this.selectedDate());
            },
            error: (err: any) => {
              const message = err.error?.title || 'Check-in failed.';
              this.snackBar.open(message, 'Close', { duration: 7000 });
            },
          });
      });
  }

  moveToDock(spot: OccupiedYardSpotDto): void {
    this.yardApi
      .moveTruckToDock(spot.yardSpotId, spot.appointmentId)
      .subscribe({
        next: () => {
          this.snackBar.open(
            `Truck from spot ${spot.spotNumber} is moving to the dock.`,
            'OK',
            { duration: 5000 }
          );
          this.loadYardData(this.selectedDate());
        },
        error: (err: any) =>
          this.snackBar.open(
            `Failed to move truck: ${
              err.error?.title || 'An unknown error occurred.'
            }`,
            'Close',
            { duration: 7000 }
          ),
      });
  }
  vacateSpot(spot: OccupiedYardSpotDto): void {
    if (confirm(`Are you sure you want to vacate spot ${spot.spotNumber}? This will remove the truck from the yard.`)) {
      this.yardApi.vacateYardSpot(spot.yardSpotId).subscribe({
        next: () => {
          this.snackBar.open(`Spot ${spot.spotNumber} vacated successfully.`, 'OK', { duration: 5000 });
          this.loadYardData(this.selectedDate());
        },
        error: (err: any) => {
          this.snackBar.open(`Failed to vacate spot: ${err.error?.title || 'Unknown error'}`, 'Close', { duration: 7000 });
        }
      });
    }
  }

  openManifestDialog(appointment: DisplayableYardAppointmentDto): void {
    this.dialog.open(AddManifestDialogComponent, {
      data: { appointmentId: appointment.appointmentId },
      width: '600px'
    });
  }
}
