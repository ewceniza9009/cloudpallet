import { Component, OnInit, Inject, inject, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidatorFn,
  ValidationErrors,
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
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { filter, switchMap, tap } from 'rxjs';
import {
  InventoryApiService,
  PalletTypeDto,
  PalletLineItemDto,
} from '../inventory-api.service';

export interface TransferItemsDialogData {
  sourcePalletId: string;
  sourcePalletBarcode: string;
  sourceInventoryLines: PalletLineItemDto[];
}

/**
 * Custom Validator: Ensures the quantity to move is valid relative to the max quantity available.
 */
function quantityValidator(maxQuantity: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const quantity = control.value;
    if (quantity === null || quantity === undefined) return null;
    if (quantity > maxQuantity)
      return { maxQuantityExceeded: { max: maxQuantity } };
    if (quantity <= 0) return { minQuantityRequired: true };
    return null;
  };
}

@Component({
  selector: 'app-transfer-items-dialog',
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
    DecimalPipe,
  ],
  templateUrl: './transfer-items-dialog.component.html',
  styleUrls: ['./transfer-items-dialog.component.scss'],
})
export class TransferItemsDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);
  public dialogRef = inject(MatDialogRef<TransferItemsDialogComponent>);

  palletTypes = signal<PalletTypeDto[]>([]);
  isLoading = signal(true);
  isSubmitting = signal(false);

  selectedInventoryLine = signal<PalletLineItemDto | null>(null);

  form: FormGroup;

  constructor(@Inject(MAT_DIALOG_DATA) public data: TransferItemsDialogData) {
    this.form = this.fb.group({
      sourceInventoryBarcode: ['', Validators.required],
      quantityToMove: [null, [Validators.required, Validators.min(1)]],

      newPalletTypeId: ['', Validators.required],

      weighedWeight: [null as number | null],
    });
  }

  ngOnInit(): void {
    this.loadPalletTypes();
  }

  loadPalletTypes(): void {
    this.inventoryApi.getPalletTypes().subscribe({
      next: (data) => {
        this.palletTypes.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load pallet types. Check API.', 'Close');
        this.isLoading.set(false);
      },
    });
  }

  /**
   * Called when a material line is selected from the dropdown.
   * @param inventoryId The unique GUID (string) of the MaterialInventory.Id selected.
   */
  onInventorySelection(inventoryId: string): void {
    const line = this.data.sourceInventoryLines.find(
      (l) => l.inventoryId === inventoryId
    );

    if (line) {
      this.selectedInventoryLine.set(line);

      this.form.controls['sourceInventoryBarcode'].setValue(inventoryId);

      this.form.controls['quantityToMove'].setValidators([
        Validators.required,
        quantityValidator(line.quantity),
      ]);
      this.form.controls['quantityToMove'].updateValueAndValidity();
    } else {
      this.selectedInventoryLine.set(null);
      this.form.controls['quantityToMove'].clearValidators();
      this.form.controls['quantityToMove'].updateValueAndValidity();
    }
  }

  onConfirm(): void {
    if (this.form.invalid || !this.selectedInventoryLine()) {
      this.snackBar.open(
        'Please select an inventory line and complete all required fields.',
        'Close'
      );
      return;
    }

    if (
      this.form.value.quantityToMove >= this.selectedInventoryLine()!.quantity
    ) {
      this.snackBar.open(
        'Cannot transfer the entire quantity. Use Pallet Transfer instead.',
        'Close'
      );
      return;
    }

    this.isSubmitting.set(true);

    const formValue = this.form.getRawValue();
    const selectedLine = this.selectedInventoryLine()!;

    this.inventoryApi
      .transferItemsToNewPallet({
        sourceInventoryId: selectedLine.inventoryId,
        quantityToMove: formValue.quantityToMove,
        newPalletTypeId: formValue.newPalletTypeId,
        weighedWeight: formValue.weighedWeight,
      })
      .subscribe({
        next: (newPalletId: string) => {
          this.dialogRef.close({ newPalletId: newPalletId });
        },
        error: (err: any) => {
          const message =
            err.error?.title ||
            err.error?.detail ||
            'Material transfer failed due to an unknown error.';
          this.snackBar.open(message, 'Close', { duration: 7000 });
          this.isSubmitting.set(false);
        },
      });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
