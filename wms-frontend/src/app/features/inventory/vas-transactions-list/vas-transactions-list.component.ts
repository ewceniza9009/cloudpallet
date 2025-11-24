import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { environment } from '../../../../environments/environment';
import { InventoryApiService, VasTransactionDto } from '../inventory-api.service';
import { AmendVasDialogComponent } from '../amend-vas-dialog/amend-vas-dialog.component';

interface AccountDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-vas-transactions-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatTableModule,
    MatIconModule,
    MatChipsModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './vas-transactions-list.component.html',
  styleUrls: ['./vas-transactions-list.component.scss']
})
export class VasTransactionsListComponent implements OnInit {
  private http = inject(HttpClient);
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private fb = inject(FormBuilder);

  filterForm: FormGroup;
  transactions = signal<VasTransactionDto[]>([]);
  accounts = signal<AccountDto[]>([]);
  isLoading = signal(false);

  displayedColumns = ['timestamp', 'serviceType', 'description', 'userName', 'status', 'actions'];

  constructor() {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 30); // Default: last 30 days

    this.filterForm = this.fb.group({
      account: [null, Validators.required],
      startDate: [startDate, Validators.required],
      endDate: [endDate, Validators.required],
      includeVoided: [false]
    });
  }

  ngOnInit(): void {
    this.loadAccounts();
  }

  loadAccounts(): void {
    this.http.get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe(accounts => this.accounts.set(accounts));
  }

  loadTransactions(): void {
    if (this.filterForm.invalid) {
      this.snackBar.open('Please select an account and date range', 'Close', { duration: 3000 });
      return;
    }

    const { account, startDate, endDate, includeVoided } = this.filterForm.value;
    this.isLoading.set(true);

    this.inventoryApi.getVasTransactions(account.id, startDate, endDate, includeVoided)
      .subscribe({
        next: (data) => {
          this.transactions.set(data);
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.snackBar.open(`Error loading transactions: ${err.error?.title || 'Unknown error'}`, 'Close');
          this.isLoading.set(false);
        }
      });
  }

  openAmendDialog(transaction: VasTransactionDto): void {
    if (transaction.isVoided) {
      this.snackBar.open('Cannot amend a voided transaction', 'Close', { duration: 3000 });
      return;
    }

    const dialogRef = this.dialog.open(AmendVasDialogComponent, {
      width: '800px',
      data: { transactionId: transaction.id }
    });

    dialogRef.afterClosed().subscribe((result: string | undefined) => {
      if (result === 'success') {
        this.loadTransactions(); // Reload to show updated data
      }
    });
  }

  voidTransaction(transaction: VasTransactionDto): void {
    if (transaction.isVoided) {
      this.snackBar.open('Transaction is already voided', 'Close', { duration: 3000 });
      return;
    }

    const reason = prompt('Enter reason for voiding this transaction:');
    if (!reason) return;

    this.inventoryApi.voidVasTransaction(transaction.id, reason)
      .subscribe({
        next: () => {
          this.snackBar.open('Transaction voided successfully', 'OK', { duration: 3000 });
          this.loadTransactions();
        },
        error: (err: HttpErrorResponse) => {
          this.snackBar.open(`Error: ${err.error?.title || 'Failed to void transaction'}`, 'Close');
        }
      });
  }

  displayAccount(account: AccountDto): string {
    return account?.name || '';
  }
}
