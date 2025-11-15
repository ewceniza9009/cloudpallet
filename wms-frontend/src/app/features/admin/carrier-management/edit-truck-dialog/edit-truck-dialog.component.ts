import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  MatDialogRef,
  MAT_DIALOG_DATA,
  MatDialogModule,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AdminSetupApiService,
  TruckDto,
  CreateTruckCommand,
  UpdateTruckCommand,
} from '../../admin-setup-api.service';

export interface EditTruckDialogData {
  carrierId: string;
  truck?: TruckDto;
}

@Component({
  selector: 'app-edit-truck-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSlideToggleModule,
  ],
  templateUrl: './edit-truck-dialog.component.html',
  styleUrls: ['./edit-truck-dialog.component.scss'],
})
export class EditTruckDialogComponent {
  private fb = inject(FormBuilder);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  public dialogRef = inject(MatDialogRef<EditTruckDialogComponent>);

  isSaving = signal(false);
  isEditMode = signal(false);
  editForm: FormGroup;

  constructor(@Inject(MAT_DIALOG_DATA) public data: EditTruckDialogData) {
    this.isEditMode.set(!!data.truck);

    this.editForm = this.fb.group({
      licensePlate: ['', Validators.required],
      model: ['', Validators.required],
      capacityWeight: [1000, [Validators.required, Validators.min(0)]],
      capacityVolume: [0, [Validators.required, Validators.min(0)]],
      isActive: [true],
    });

    if (this.isEditMode()) {
      this.editForm.patchValue(data.truck!);
    }
  }

  onConfirm(): void {
    if (this.editForm.invalid) return;
    this.isSaving.set(true);
    const formValue = this.editForm.value;

    if (this.isEditMode()) {
      const command: UpdateTruckCommand = {
        ...this.data.truck!,
        ...formValue,
      };
      this.adminSetupApi.updateTruck(command.id, command).subscribe({
        next: () => this.handleSuccess('Truck updated successfully.'),
        error: (err) => this.handleError(err),
      });
    } else {
      const command: CreateTruckCommand = {
        carrierId: this.data.carrierId,
        ...formValue,
      };
      this.adminSetupApi.createTruck(command).subscribe({
        next: () => this.handleSuccess('Truck created successfully.'),
        error: (err) => this.handleError(err),
      });
    }
  }

  private handleSuccess(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 3000 });
    this.isSaving.set(false);
    this.dialogRef.close(true);
  }

  private handleError(err: any): void {
    this.snackBar.open(
      `Error: ${err.error?.title || 'Failed to save truck.'}`,
      'Close'
    );
    this.isSaving.set(false);
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
