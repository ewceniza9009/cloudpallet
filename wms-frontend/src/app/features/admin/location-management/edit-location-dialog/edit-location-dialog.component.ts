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
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AdminSetupApiService,
  LocationDto,
  LocationType,
  UpdateLocationCommand,
} from '../../admin-setup-api.service';

export interface EditLocationDialogData {
  location: LocationDto;
}

@Component({
  selector: 'app-edit-location-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSlideToggleModule,
  ],
  templateUrl: './edit-location-dialog.component.html',
  styleUrls: ['./edit-location-dialog.component.scss'],
})
export class EditLocationDialogComponent {
  private fb = inject(FormBuilder);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  public dialogRef = inject(MatDialogRef<EditLocationDialogComponent>);

  isSaving = signal(false);
  editForm: FormGroup;
  locationTypes: LocationType[] = ['Storage', 'Picking', 'Staging'];

  constructor(@Inject(MAT_DIALOG_DATA) public data: EditLocationDialogData) {
    this.editForm = this.fb.group({
      zoneType: [data.location.zoneType, Validators.required],
      capacityWeight: [
        data.location.capacityWeight,
        [Validators.required, Validators.min(0)],
      ],
      isActive: [data.location.isActive],
    });

    if (data.location.zoneType === 'Storage') {
      this.editForm.get('zoneType')?.disable();
    }
  }

  onConfirm(): void {
    if (this.editForm.invalid) {
      return;
    }

    this.isSaving.set(true);
    const formValue = this.editForm.getRawValue();

    const command: UpdateLocationCommand = {
      locationId: this.data.location.id,
      zoneType: formValue.zoneType,
      capacityWeight: formValue.capacityWeight,
      isActive: formValue.isActive,
    };

    this.adminSetupApi.updateLocation(command.locationId, command).subscribe({
      next: () => {
        this.snackBar.open('Location updated successfully.', 'OK', {
          duration: 3000,
        });
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.snackBar.open(
          `Error: ${err.error?.title || 'Failed to update location.'}`,
          'Close',
          { duration: 5000 }
        );
        this.isSaving.set(false);
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
