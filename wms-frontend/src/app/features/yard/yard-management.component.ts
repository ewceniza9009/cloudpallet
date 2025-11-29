import { Component, OnInit, inject, signal, effect } from '@angular/core';
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
  ],
  templateUrl: './yard-management.component.html',
  styleUrls: ['./yard-management.component.scss'],
})
export class YardManagementComponent implements OnInit {
  private yardApi = inject(YardApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  pendingAppointments = signal<DisplayableYardAppointmentDto[]>([]);
  occupiedSpots = signal<OccupiedYardSpotDto[]>([]);
  isLoading = signal(true);

  arrivalsSearchControl = new FormControl('');
  yardSearchControl = new FormControl('');

  filteredPendingAppointments = signal<DisplayableYardAppointmentDto[]>([]);
  filteredOccupiedSpots = signal<OccupiedYardSpotDto[]>([]);

  constructor() {
    effect(() => {
      const appointments = this.pendingAppointments();
      const filterValue = this.arrivalsSearchControl.value?.toLowerCase() || '';
      if (!filterValue) {
        this.filteredPendingAppointments.set(appointments);
        return;
      }
      this.filteredPendingAppointments.set(
        appointments.filter(
          (apt) =>
            apt.licensePlate.toLowerCase().includes(filterValue) ||
            apt.carrierName.toLowerCase().includes(filterValue) ||
            apt.dockName.toLowerCase().includes(filterValue)
        )
      );
    });

    effect(() => {
      const spots = this.occupiedSpots();
      const filterValue = this.yardSearchControl.value?.toLowerCase() || '';
      if (!filterValue) {
        this.filteredOccupiedSpots.set(spots);
        return;
      }
      this.filteredOccupiedSpots.set(
        spots.filter(
          (spot) =>
            spot.licensePlate.toLowerCase().includes(filterValue) ||
            spot.spotNumber.toLowerCase().includes(filterValue)
        )
      );
    });
  }

  ngOnInit(): void {
    this.loadYardData();
  }

  loadYardData(): void {
    this.isLoading.set(true);
    const now = new Date();
    const startOfDay = new Date(
      Date.UTC(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0)
    );
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

        this.arrivalsSearchControl.setValue(this.arrivalsSearchControl.value);
        this.yardSearchControl.setValue(this.yardSearchControl.value);
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
              this.loadYardData();
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
          this.loadYardData();
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
          this.loadYardData();
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
