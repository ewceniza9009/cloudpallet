import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon'; // ADD THIS
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner'; // ADD THIS
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { InventoryApiService, WeightDto } from '../../inventory/inventory-api.service'; // ADD THIS
import { MatSnackBar } from '@angular/material/snack-bar'; // ADD THIS

export interface ScanDialogData {
  pickId: string;
  material: string;
  location: string;
}

@Component({
  selector: 'app-scan-confirmation-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatButtonModule, CdkTrapFocus,
    MatIconModule, MatProgressSpinnerModule // ADD THESE
  ],
  templateUrl: './scan-confirmation-dialog.component.html',
  styleUrls: ['./scan-confirmation-dialog.component.scss'] // ADD THIS
})
export class ScanConfirmationDialogComponent {
  scanForm: FormGroup;
  isWeighing = signal(false);

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<ScanConfirmationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ScanDialogData,
    private inventoryApi: InventoryApiService, // ADD THIS
    private snackBar: MatSnackBar // ADD THIS
  ) {
    this.scanForm = this.fb.group({
      scannedLocationCode: ['', Validators.required],
      scannedLpn: ['', Validators.required],
      actualWeight: [null as number | null, Validators.required] // ADD THIS
    });
  }

  onWeigh(): void {
    this.isWeighing.set(true);
    this.inventoryApi.getScaleWeight().subscribe({
      next: (weightDto: WeightDto) => {
        this.scanForm.patchValue({ actualWeight: weightDto.value });
        this.isWeighing.set(false);
        this.snackBar.open(`Weight captured: ${weightDto.value} KG`, 'OK', { duration: 2000 });
      },
      error: () => {
        this.isWeighing.set(false);
        this.snackBar.open('Failed to get weight from scale.', 'Close');
      }
    });
  }

  onConfirm(): void {
    if (this.scanForm.valid) {
      this.dialogRef.close(this.scanForm.value);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
