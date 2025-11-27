import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { map, startWith } from 'rxjs';

interface AppointmentDto { id: string; licensePlate: string; startTime: string; }

@Component({
  selector: 'app-select-appointment-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    MatIconModule,
  ],
  templateUrl: './select-appointment-dialog.component.html',
  styleUrls: ['./select-appointment-dialog.component.scss']
})
export class SelectAppointmentDialogComponent implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  public dialogRef = inject(MatDialogRef<SelectAppointmentDialogComponent>);

  appointments = signal<AppointmentDto[]>([]);
  filteredAppointments = signal<AppointmentDto[]>([]);
  isLoading = signal(true);

  form: FormGroup;

  constructor() {
    this.form = this.fb.group({
      appointmentId: ['', Validators.required],
      appointmentSearch: [''] // Control for the autocomplete input
    });
  }

  ngOnInit(): void {
    this.http.get<AppointmentDto[]>(`${environment.apiUrl}/Lookups/outbound-appointments`).subscribe({
      next: (data) => {
        this.appointments.set(data);
        this.isLoading.set(false);
        this.setupFilter();
      },
      error: () => this.isLoading.set(false)
    });
  }

  private setupFilter(): void {
    this.form.get('appointmentSearch')?.valueChanges.pipe(
      startWith(''),
      map(value => this._filter(value || ''))
    ).subscribe(filtered => this.filteredAppointments.set(filtered));
  }

  private _filter(value: string): AppointmentDto[] {
    const filterValue = value.toLowerCase();
    return this.appointments().filter(apt => apt.licensePlate.toLowerCase().includes(filterValue));
  }

  displayAppointmentName(appointment: AppointmentDto): string {
    return appointment?.licensePlate || '';
  }

  onAppointmentSelected(appointment: AppointmentDto): void {
    this.form.get('appointmentId')?.setValue(appointment.id);
  }

  onConfirm(): void {
    if (this.form.valid) {
      // Return only the appointmentId, as the parent component expects
      this.dialogRef.close({ appointmentId: this.form.value.appointmentId });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
