// ---- File: wms-frontend/src/app/features/billing/billing-api.service.ts [UPDATED] ----

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Invoice {
  id: string;
  invoiceNumber: string;
  periodStart: string;
  periodEnd: string;
  dueDate: string;
  totalAmount: number;
  status: 'Draft' | 'Issued' | 'Paid' | 'Overdue';
}

export interface InvoiceLine {
  id: string;
  description: string;
  quantity: number;
  unitRate: number;
  amount: number;
}

export interface InvoiceDetailDto {
  id: string;
  name: string; // Account Name
  invoiceNumber: string;
  periodStart: string;
  periodEnd: string;
  dueDate: string;
  totalAmount: number;
  taxAmount: number;
  status: 'Draft' | 'Issued' | 'Paid' | 'Overdue';
  lines: InvoiceLine[];
}

export interface GenerateInvoiceCommand {
  accountId: string;
  periodStart: string; // ISO date string
  periodEnd: string; // ISO date string
}

@Injectable({
  providedIn: 'root'
})
export class BillingApiService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Billing`;

  getInvoices(accountId: string): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(this.apiUrl, { params: { accountId } });
  }

  getInvoiceById(id: string): Observable<InvoiceDetailDto> {
    return this.http.get<InvoiceDetailDto>(`${this.apiUrl}/${id}`);
  }

  generateInvoice(command: GenerateInvoiceCommand): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/generate`, command);
  }

  // --- START: NEW METHOD ---
  getInvoicePdf(invoiceId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${invoiceId}/pdf`, {
      responseType: 'blob' // Important: Expect a file blob
    });
  }
  // --- END: NEW METHOD ---
}
