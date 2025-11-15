// ---- File: wms-frontend/src/app/features/admin/uom-setup/uom-dialog/uom-dialog.component.ts [NEW FILE] ----

import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminSetupApiService, UnitOfMeasureDetailDto, CreateUnitOfMeasureCommand, UpdateUnitOfMeasureCommand } from '../../admin-setup-api.service';

export interface UomDialogData {
  uom?: UnitOfMeasureDetailDto;
}

@Component({
  selector: 'app-uom-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
    MatIconModule, MatSlideToggleModule
  ],
  templateUrl: './uom-dialog.component.html',
  styleUrls: ['./uom-dialog.component.scss']
})
export class UomDialogComponent {
  private fb = inject(FormBuilder);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  public dialogRef = inject(MatDialogRef<UomDialogComponent>);

  isSaving = signal(false);
  isEditMode = signal(false);
  editForm: FormGroup;

  constructor(@Inject(MAT_DIALOG_DATA) public data: UomDialogData) {
    this.isEditMode.set(!!data.uom);

    this.editForm = this.fb.group({
      name: ['', Validators.required],
      symbol: ['', Validators.required]
    });

    if (this.isEditMode()) {
      this.editForm.patchValue(data.uom!);
    }
  }

  onConfirm(): void {
    if (this.editForm.invalid) return;
    this.isSaving.set(true);
    const formValue = this.editForm.value;

    if (this.isEditMode()) {
      const command: UpdateUnitOfMeasureCommand = {
        ...this.data.uom!,
        ...formValue
      };
      this.adminSetupApi.updateUoM(command.id, command).subscribe({
        next: () => this.handleSuccess('UoM updated successfully.'),
        error: (err) => this.handleError(err)
      });
    } else {
      const command: CreateUnitOfMeasureCommand = formValue;
      this.adminSetupApi.createUoM(command).subscribe({
        next: () => this.handleSuccess('UoM created successfully.'),
        error: (err) => this.handleError(err)
      });
    }
  }

  private handleSuccess(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 3000 });
    this.isSaving.set(false);
    this.dialogRef.close(true); // Signal success
  }

  private handleError(err: any): void {
    this.snackBar.open(`Error: ${err.error?.title || 'Failed to save.'}`, 'Close');
    this.isSaving.set(false);
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
