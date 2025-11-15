import { Component, OnInit, inject, signal } from '@angular/core';
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
import { switchMap, tap } from 'rxjs';

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
  ],
  templateUrl: './invoice-list.component.html',
  styleUrls: ['./invoice-list.component.scss'],
})
export class InvoiceListComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private billingApi = inject(BillingApiService);
  private snackBar = inject(MatSnackBar);

  accounts = signal<AccountDto[]>([]);
  invoices = signal<Invoice[]>([]);
  isLoading = signal(false);
  isGenerating = signal(false);

  billingForm = this.fb.group({
    accountId: ['', Validators.required],
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
      .subscribe((data) => this.accounts.set(data));

    this.billingForm
      .get('accountId')
      ?.valueChanges.pipe(
        tap(() => this.isLoading.set(true)),
        switchMap((accountId) => this.billingApi.getInvoices(accountId!))
      )
      .subscribe((data: Invoice[]) => {
        this.invoices.set(data);
        this.isLoading.set(false);
      });
  }

  generateInvoice(): void {
    if (this.billingForm.invalid) {
      this.snackBar.open(
        'Please select an account and a valid date range.',
        'Close'
      );
      return;
    }

    this.isGenerating.set(true);

    const formValue = this.billingForm.getRawValue();
    const command = {
      accountId: formValue.accountId!,
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
    const accountId = this.billingForm.get('accountId')?.value;
    if (accountId) {
      this.isLoading.set(true);
      this.billingApi.getInvoices(accountId).subscribe((data: Invoice[]) => {
        this.invoices.set(data);
        this.isLoading.set(false);
      });
    }
  }
}
