import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpErrorResponse } from '@angular/common/http';
import { InventoryApiService, VasTransactionDetailDto, VasTransactionLineDto } from '../inventory-api.service';

@Component({
  selector: 'app-amend-vas-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatTabsModule,
    MatExpansionModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './amend-vas-dialog.component.html',
  styleUrls: ['./amend-vas-dialog.component.scss']
})
export class AmendVasDialogComponent implements OnInit {
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  transaction = signal<VasTransactionDetailDto | null>(null);
  isLoading = signal(true);
  amendmentForms = new Map<string, FormGroup>();

  constructor(
    public dialogRef: MatDialogRef<AmendVasDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { transactionId: string }
  ) {}

  ngOnInit(): void {
    this.loadTransactionDetails();
  }

  loadTransactionDetails(): void {
    this.inventoryApi.getVasTransactionDetails(this.data.transactionId)
      .subscribe({
        next: (tx) => {
          this.transaction.set(tx);
          this.isLoading.set(false);
          
          // Create amendment forms for each line
          [...tx.inputLines, ...tx.outputLines].forEach(line => {
            this.amendmentForms.set(line.id, this.fb.group({
              newQuantity: [line.quantity],
              newWeight: [line.weight],
              reason: ['', Validators.required]
            }));
          });
        },
        error: (err: HttpErrorResponse) => {
          this.snackBar.open(`Error loading transaction: ${err.error?.title || 'Unknown error'}`, 'Close');
          this.isLoading.set(false);
        }
      });
  }

  amendLine(line: VasTransactionLineDto): void {
    const form = this.amendmentForms.get(line.id);
    if (!form || form.invalid) {
      this.snackBar.open('Please fill in the reason for amendment', 'Close', { duration: 3000 });
      return;
    }

    const { newQuantity, newWeight, reason } = form.value;
    
    // Check if values actually changed
    if (newQuantity === line.quantity && newWeight === line.weight) {
      this.snackBar.open('No changes made to quantity or weight', 'Close', { duration: 3000 });
      return;
    }

    this.inventoryApi.amendVasTransactionLine(
      this.data.transactionId,
      line.id,
      newQuantity !== line.quantity ? newQuantity : null,
      newWeight !== line.weight ? newWeight : null,
      reason
    ).subscribe({
      next: () => {
        this.snackBar.open('Line amended successfully!', 'OK', { duration: 3000 });
        this.loadTransactionDetails(); // Reload to show changes
      },
      error: (err: HttpErrorResponse) => {
        this.snackBar.open(`Error: ${err.error?.title || 'Failed to amend line'}`, 'Close');
      }
    });
  }

  close(result?: string): void {
    this.dialogRef.close(result);
  }
}
