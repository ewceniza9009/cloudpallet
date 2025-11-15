import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
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
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import {
  InventoryApiService,
  WeightDto,
} from '../../inventory/inventory-api.service';
import { MatSnackBar } from '@angular/material/snack-bar';

export interface ExistingLineData {
  quantity?: number;
  batchNumber?: string;
  dateOfManufacture?: string;
  expiryDate?: string;
}

export interface ProcessLineDialogData {
  palletLineId: string;
  receivingId: string;
  palletId: string;
  materialId: string;
  materialName: string;
  palletTareWeight: number;
  existingData?: ExistingLineData;
}

export interface ProcessLineDialogResult {
  barcode: string;
  netWeight: number;
  quantity: number;
  batchNumber: string;
  dateOfManufacture: string;
  expiryDate: string | null;
}

@Component({
  selector: 'app-process-line-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatDividerModule,
    MatListModule,
  ],
  templateUrl: './process-line-dialog.component.html',
  styleUrls: ['./process-line-dialog.component.scss'],
})
export class ProcessLineDialogComponent {
  private fb = inject(FormBuilder);
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);
  isWeighing = signal(false);
  isSubmitting = signal(false);
  grossWeight = signal<number | null>(null);
  netWeight = signal<number | null>(null);
  infoForm: FormGroup;
  weighingForm: FormGroup;

  constructor(
    public dialogRef: MatDialogRef<
      ProcessLineDialogComponent,
      ProcessLineDialogResult
    >,
    @Inject(MAT_DIALOG_DATA) public data: ProcessLineDialogData
  ) {
    this.infoForm = this.fb.group({
      batchNumber: ['', Validators.required],
      dateOfManufacture: ['', Validators.required],
      expiryDate: [''],
      quantity: [1, [Validators.required, Validators.min(1)]],
    });

    this.weighingForm = this.fb.group({
      grossWeight: [{ value: null, disabled: true }, Validators.required],
      netWeight: [{ value: null, disabled: true }, Validators.required],
    });

    if (this.data.existingData) {
      this.infoForm.patchValue({
        batchNumber: this.data.existingData.batchNumber,
        dateOfManufacture: this.data.existingData.dateOfManufacture
          ? formatDate(
              this.data.existingData.dateOfManufacture,
              'yyyy-MM-dd',
              'en-US'
            )
          : '',
        expiryDate: this.data.existingData.expiryDate
          ? formatDate(this.data.existingData.expiryDate, 'yyyy-MM-dd', 'en-US')
          : '',
        quantity: this.data.existingData.quantity,
      });
    }
  }

  onWeigh(): void {
    this.isWeighing.set(true);
    this.inventoryApi.getScaleWeight().subscribe({
      next: (weightDto: WeightDto) => {
        const gross = weightDto.value;
        const net = gross - this.data.palletTareWeight;
        this.grossWeight.set(gross);
        this.netWeight.set(net);
        this.weighingForm.patchValue({ grossWeight: gross, netWeight: net });
        this.isWeighing.set(false);
      },
      error: (err: any) => {
        this.isWeighing.set(false);
        this.snackBar.open('Failed to get weight from scale.', 'Close', {
          duration: 5000,
        });
      },
    });
  }

  onSubmit(): void {
    if (this.infoForm.invalid || this.weighingForm.invalid) {
      this.snackBar.open(
        'Please complete all steps before submitting.',
        'Close'
      );
      return;
    }
    this.isSubmitting.set(true);
    const info = this.infoForm.value;
    const command = {
      receivingId: this.data.receivingId,
      palletId: this.data.palletId,
      palletLineId: this.data.palletLineId,
      materialId: this.data.materialId,
      quantity: info.quantity,
      batchNumber: info.batchNumber,
      dateOfManufacture: info.dateOfManufacture,
      expiryDate: info.expiryDate || null,
      grossWeight: this.grossWeight()!,
      userId: '10000000-0000-0000-0000-000000000001', // Placeholder user ID
    };
    this.inventoryApi.processPalletLine(command).subscribe({
      next: (barcode: string) => {
        this.isSubmitting.set(false);
        this.dialogRef.close({
          barcode: barcode,
          netWeight: this.netWeight()!,
          quantity: info.quantity,
          batchNumber: info.batchNumber,
          dateOfManufacture: info.dateOfManufacture,
          expiryDate: info.expiryDate || null,
        });
      },
      error: (err: any) => {
        console.log(err);
        this.isSubmitting.set(false);
        const errorMessage =
          err.error?.title || err.error?.detail || 'Unknown processing error';
        this.snackBar.open(`Processing failed: ${errorMessage}`, 'Close');
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
