import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DockAppointmentDto } from '../../../core/models/dock-appointment.dto';

@Component({
  selector: 'app-appointment-details-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>Appointment Details</h2>
    <mat-dialog-content>
      <div class="details-grid">
        <div class="detail-item">
          <span class="label">License Plate</span>
          <span class="value">{{ data.licensePlate || 'Unknown' }}</span>
        </div>
        <div class="detail-item">
          <span class="label">Dock</span>
          <span class="value">{{ data.dockName }}</span>
        </div>
        <div class="detail-item">
          <span class="label">Status</span>
          <span class="value status-chip" [ngClass]="'status-' + data.status.toLowerCase()">
            {{ data.status }}
          </span>
        </div>
        <div class="detail-item">
          <span class="label">Start Time</span>
          <span class="value">{{ data.startDateTime | date:'medium' }}</span>
        </div>
        <div class="detail-item">
          <span class="label">End Time</span>
          <span class="value">{{ data.endDateTime | date:'medium' }}</span>
        </div>
        <!-- Add more fields as needed -->
      </div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .details-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      padding-top: 8px;
    }
    .detail-item {
      display: flex;
      flex-direction: column;
    }
    .label {
      font-size: 0.75rem;
      color: #64748b;
      font-weight: 500;
      text-transform: uppercase;
      margin-bottom: 4px;
    }
    .value {
      font-size: 1rem;
      color: #1e293b;
      font-weight: 500;
    }
    .status-chip {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 0.85rem;
      font-weight: 600;
      text-transform: uppercase;
      width: fit-content;
      
      &.status-scheduled { background-color: #e8eaf6; color: #1a237e; }
      &.status-arrived { background-color: #fff3e0; color: #e65100; }
      &.status-loading { background-color: #e8f5e9; color: #1b5e20; }
      &.status-completed { background-color: #f5f5f5; color: #424242; }
      &.status-cancelled { background-color: #ffebee; color: #b71c1c; }
      &.status-inprogress { background-color: #e3f2fd; color: #0d47a1; }
    }
  `]
})
export class AppointmentDetailsDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<AppointmentDetailsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DockAppointmentDto
  ) {}
}
