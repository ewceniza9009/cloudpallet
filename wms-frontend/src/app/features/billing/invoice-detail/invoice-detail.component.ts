import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BillingApiService, InvoiceDetailDto } from '../billing-api.service';
import { switchMap } from 'rxjs';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    CurrencyPipe,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './invoice-detail.component.html',
  styleUrls: ['./invoice-detail.component.scss'],
})
export class InvoiceDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private billingApi = inject(BillingApiService);
  private snackBar = inject(MatSnackBar);

  invoice = signal<InvoiceDetailDto | null>(null);
  isLoading = signal(true);

  invoiceId = signal<string>('');
  isLoadingPdf = signal(false);

  displayedColumns: string[] = [
    'description',
    'quantity',
    'unitRate',
    'amount',
  ];

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (!id) {
            this.isLoading.set(false);
            throw new Error('No invoice ID provided');
          }
          this.invoiceId.set(id);
          return this.billingApi.getInvoiceById(id);
        })
      )
      .subscribe({
        next: (data) => {
          this.invoice.set(data);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
  }

  loadPdfPreview(): void {
    this.isLoadingPdf.set(true);

    this.billingApi.getInvoicePdf(this.invoiceId()).subscribe({
      next: (blob) => {
        const file = new Blob([blob], { type: 'application/pdf' });
        const fileURL = URL.createObjectURL(file);

        window.open(fileURL, '_blank');

        this.isLoadingPdf.set(false);
      },
      error: (err) => {
        console.error(err);
        this.snackBar.open('Failed to load PDF preview.', 'Close', {
          duration: 5000,
        });
        this.isLoadingPdf.set(false);
      },
    });
  }
}
