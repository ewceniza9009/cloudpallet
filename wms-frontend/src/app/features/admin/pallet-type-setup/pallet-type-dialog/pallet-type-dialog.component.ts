
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
import { AdminSetupApiService, PalletTypeDetailDto, CreatePalletTypeCommand, UpdatePalletTypeCommand } from '../../admin-setup-api.service';

export interface PalletTypeDialogData {
  palletType?: PalletTypeDetailDto;
}

@Component({
  selector: 'app-pallet-type-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
    MatIconModule, MatSlideToggleModule
  ],
  templateUrl: './pallet-type-dialog.component.html',
  styleUrls: ['./pallet-type-dialog.component.scss']
})
export class PalletTypeDialogComponent {
  private fb = inject(FormBuilder);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  public dialogRef = inject(MatDialogRef<PalletTypeDialogComponent>);

  isSaving = signal(false);
  isEditMode = signal(false);
  editForm: FormGroup;

  constructor(@Inject(MAT_DIALOG_DATA) public data: PalletTypeDialogData) {
    this.isEditMode.set(!!data.palletType);

    this.editForm = this.fb.group({
      name: ['', Validators.required],
      tareWeight: [null, [Validators.required, Validators.min(0)]],
      length: [null, [Validators.required, Validators.min(0.01)]],
      width: [null, [Validators.required, Validators.min(0.01)]],
      height: [null, [Validators.required, Validators.min(0.01)]],
      isActive: [true]
    });

    if (this.isEditMode()) {
      this.editForm.patchValue(data.palletType!);
    } else {

        this.editForm.get('isActive')?.setValue(true);
    }
  }

  onConfirm(): void {
    if (this.editForm.invalid) return;
    this.isSaving.set(true);
    const formValue = this.editForm.getRawValue();

    if (this.isEditMode()) {
      const command: UpdatePalletTypeCommand = {
        ...this.data.palletType!,
        ...formValue
      };
      this.adminSetupApi.updatePalletType(command.id, command).subscribe({
        next: () => this.handleSuccess('Pallet Type updated successfully.'),
        error: (err) => this.handleError(err)
      });
    } else {
      const command: CreatePalletTypeCommand = formValue;
      this.adminSetupApi.createPalletType(command).subscribe({
        next: () => this.handleSuccess('Pallet Type created successfully.'),
        error: (err) => this.handleError(err)
      });
    }
  }

  private handleSuccess(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 3000 });
    this.isSaving.set(false);
    this.dialogRef.close(true);
  }

  private handleError(err: any): void {
    const errorMsg = err.error?.title || err.error?.detail || 'Failed to save Pallet Type.';
    this.snackBar.open(`Error: ${errorMsg}`, 'Close', { duration: 5000 });
    this.isSaving.set(false);
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
