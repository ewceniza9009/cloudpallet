import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import {
  InventoryApiService,
  PalletTypeDto,
} from '../../inventory/inventory-api.service';

export interface SelectPalletTypeResult {
  palletTypeId: string;
  isCrossDock: boolean;
}

@Component({
  selector: 'app-select-pallet-type-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
    MatIconModule,
  ],
  templateUrl: './select-pallet-type-dialog.component.html',
  styleUrls: ['./select-pallet-type-dialog.component.scss'],
})
export class SelectPalletTypeDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private inventoryApi = inject(InventoryApiService);

  public dialogRef = inject(
    MatDialogRef<SelectPalletTypeDialogComponent, SelectPalletTypeResult>
  );

  palletTypes = signal<PalletTypeDto[]>([]);
  isLoading = signal(true);

  form = this.fb.group({
    palletTypeId: ['', Validators.required],
    isCrossDock: [false],
  });

  ngOnInit(): void {
    this.inventoryApi.getPalletTypes().subscribe({
      next: (data) => {
        this.palletTypes.set(data);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  onConfirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value as SelectPalletTypeResult);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
