import { Component, OnInit, inject, signal, ViewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { ScrollingModule, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { switchMap, tap, startWith, map, Observable } from 'rxjs';

import { BillingApiService, Invoice } from '../billing-api.service';
import { environment } from '../../../../environments/environment';

interface AccountDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DatePipe,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatAutocompleteModule,
    ScrollingModule,
  ],
  templateUrl: './invoice-list.component.html',
  styleUrls: ['./invoice-list.component.scss'],
})
export class InvoiceListComponent implements OnInit {
  @ViewChild(CdkVirtualScrollViewport)
  virtualScrollViewport!: CdkVirtualScrollViewport;

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private billingApi = inject(BillingApiService);
  private snackBar = inject(MatSnackBar);

  accounts = signal<AccountDto[]>([]);
  filteredAccounts!: Observable<AccountDto[]>;
  invoices = signal<Invoice[]>([]);
  isLoading = signal(false);
  isGenerating = signal(false);

  billingForm = this.fb.group({
    accountId: [null as string | AccountDto | null, Validators.required],
    period: this.fb.group({
      start: [new Date(2025, 9, 1), Validators.required],
      end: [new Date(2025, 9, 31), Validators.required],
    }),
  });

  displayedColumns = [
    'invoiceNumber',
    'period',
    'dueDate',
    'totalAmount',
    'status',
    'actions',
  ];

  ngOnInit(): void {
    this.http
      .get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe((data) => {
        this.accounts.set(data);
        
        this.filteredAccounts = this.billingForm.get('accountId')!.valueChanges.pipe(
          startWith(''),
          map((value) => (typeof value === 'string' ? value : value?.name) || ''),
          map((name) => (name ? this._filter(name) : this.accounts().slice()))
        );
      });

    this.billingForm.get('accountId')?.valueChanges.pipe(
      tap(() => {
        // Only trigger loading if it's a valid selection (object), not just typing
        const val = this.billingForm.get('accountId')?.value;
        if (val && typeof val !== 'string') {
          this.isLoading.set(true);
        }
      }),
      switchMap((val) => {
        if (val && typeof val !== 'string') {
           return this.billingApi.getInvoices((val as AccountDto).id);
        }
        return [];
      })
    ).subscribe((data: Invoice[]) => {
      this.invoices.set(data);
      this.isLoading.set(false);
    });
  }

  private _filter(value: string): AccountDto[] {
    const filterValue = value.toLowerCase();
    return this.accounts().filter((option) =>
      option.name.toLowerCase().includes(filterValue)
    );
  }

  displayFn(account: AccountDto): string {
    return account && account.name ? account.name : '';
  }

  generateInvoice(): void {
    if (this.billingForm.invalid) {
      this.snackBar.open(
        'Please select an account and a valid date range.',
        'Close'
      );
      return;
    }

    const accountVal = this.billingForm.get('accountId')?.value;
    if (!accountVal || typeof accountVal === 'string') {
        this.snackBar.open('Please select a valid account from the list.', 'Close');
        return;
    }

    this.isGenerating.set(true);

    const formValue = this.billingForm.getRawValue();
    const command = {
      accountId: (accountVal as AccountDto).id,
      periodStart: formValue.period.start!.toISOString(),
      periodEnd: formValue.period.end!.toISOString(),
    };

    this.billingApi.generateInvoice(command).subscribe({
      next: (invoiceId: string) => {
        this.snackBar.open(`Successfully generated new invoice!`, 'OK', {
          duration: 5000,
        });
        this.refreshInvoices();
        this.isGenerating.set(false);
      },
      error: (err: any) => {
        this.snackBar.open('Invoice generation failed.', 'Close');
        this.isGenerating.set(false);
      },
    });
  }

  refreshInvoices(): void {
    const accountVal = this.billingForm.get('accountId')?.value;
    if (accountVal && typeof accountVal !== 'string') {
      const accountId = (accountVal as AccountDto).id;
      this.isLoading.set(true);
      this.billingApi.getInvoices(accountId).subscribe((data: Invoice[]) => {
        this.invoices.set(data);
        this.isLoading.set(false);
      });
    }
  }
}
