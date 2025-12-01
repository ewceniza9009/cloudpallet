import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

export interface InventoryLedgerLineDto {
  date: string;
  type: string;
  document: string;
  quantityIn: number;
  quantityOut: number;
  weightIn: number;
  weightOut: number;
  runningBalanceQty: number;
  runningBalanceWgt: number;
}

export interface InventoryLedgerGroupDto {
  materialId: string;
  materialName: string;
  totalQtyIn: number;
  totalQtyOut: number;
  netQtyChange: number;
  totalWgtIn: number;
  totalWgtOut: number;
  netWgtChange: number;
  lines: InventoryLedgerLineDto[];
}

export interface LedgerFilter {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  startDate?: string;
  endDate?: string;
  accountId?: string;
  materialId?: string;
  supplierId?: string;
  truckId?: string;
  userId?: string;
}

export interface ActivityLogDto {
  timestamp: string;
  user: string;
  action: string;
  description: string;
  account: string | null;
}

export interface ActivityLogFilter {
  page: number;
  pageSize: number;
  startDate?: string;
  endDate?: string;
  accountId?: string;
  userId?: string;
  truckId?: string;
}

// --- THIS IS THE CORRECTED LINE ---
export type ReportType =
  | 'Receiving'
  | 'Putaway'
  | 'Transfer'
  | 'Picking'
  | 'Invoice'
  | 'Shipping'
  | 'VAS'
  | 'VAS_Amend';

export interface ReportFilterDto {
  reportType: ReportType;
  startDate: string; // ISO string
  endDate: string; // ISO string
  accountId?: string;
  materialId?: string;
  supplierId?: string;
  userId?: string;
}

export interface StockOnHandDto {
  materialInventoryId: string;
  materialName: string;
  sku: string;
  palletBarcode: string;
  lpnBarcode: string;
  batchNumber: string;
  location: string;
  room: string;
  accountName: string;
  supplierName: string;
  quantity: number;
  weight: number;
  expiryDate?: string;
}

export interface StockOnHandFilter {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  accountId?: string;
  materialId?: string;
  supplierId?: string;
  batchNumber?: string;
  barcode?: string;
}

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Reports`;

  getStockOnHandReport(
    filter: StockOnHandFilter
  ): Observable<PagedResult<StockOnHandDto>> {
    let params = new HttpParams();
    for (const key in filter) {
      if (filter[key as keyof StockOnHandFilter]) {
        params = params.set(
          key,
          filter[key as keyof StockOnHandFilter]!.toString()
        );
      }
    }
    return this.http.get<PagedResult<StockOnHandDto>>(
      `${this.apiUrl}/stock-on-hand`,
      { params }
    );
  }

  getInventoryLedger(
    filter: LedgerFilter
  ): Observable<PagedResult<InventoryLedgerGroupDto>> {
    let params = new HttpParams();
    for (const key in filter) {
      if (filter[key as keyof LedgerFilter]) {
        params = params.set(key, filter[key as keyof LedgerFilter]!.toString());
      }
    }
    return this.http.get<PagedResult<InventoryLedgerGroupDto>>(
      `${this.apiUrl}/inventory-ledger`,
      { params }
    );
  }

  getInventoryLedgerDetails(
    filter: LedgerFilter
  ): Observable<InventoryLedgerLineDto[]> {
    let params = new HttpParams();
    for (const key in filter) {
      if (filter[key as keyof LedgerFilter]) {
        params = params.set(key, filter[key as keyof LedgerFilter]!.toString());
      }
    }
    return this.http.get<InventoryLedgerLineDto[]>(
      `${this.apiUrl}/inventory-ledger/details`,
      { params }
    );
  }

  getActivityLog(
    filter: ActivityLogFilter
  ): Observable<PagedResult<ActivityLogDto>> {
    let params = new HttpParams();
    for (const key in filter) {
      if (filter[key as keyof ActivityLogFilter]) {
        params = params.set(
          key,
          filter[key as keyof ActivityLogFilter]!.toString()
        );
      }
    }
    return this.http.get<PagedResult<ActivityLogDto>>(
      `${this.apiUrl}/activity-log`,
      { params }
    );
  }

  getCustomReport(filter: ReportFilterDto): Observable<Blob> {
    let params = new HttpParams(); // Loop and set all filter properties as query params

    for (const key in filter) {
      if (filter[key as keyof ReportFilterDto]) {
        params = params.set(
          key,
          filter[key as keyof ReportFilterDto]!.toString()
        );
      }
    }

    return this.http.get(`${this.apiUrl}/custom`, {
      params,
      responseType: 'blob', // <-- Tell Angular to expect a file Blob
    });
  }

  getCycleCountVariances(
    filter: CycleCountVarianceFilter
  ): Observable<PagedResult<CycleCountVarianceDto>> {
    let params = new HttpParams();
    for (const key in filter) {
      if (filter[key as keyof CycleCountVarianceFilter]) {
        params = params.set(
          key,
          filter[key as keyof CycleCountVarianceFilter]!.toString()
        );
      }
    }
    return this.http.get<PagedResult<CycleCountVarianceDto>>(
      `${this.apiUrl}/cycle-count-variances`,
      { params }
    );
  }
}

export interface CycleCountVarianceDto {
  adjustmentId: string;
  timestamp: string;
  materialName: string;
  sku: string;
  locationName: string;
  palletBarcode: string;
  varianceQuantity: number;
  varianceValue: number;
  userName: string;
  accountName: string;
}

export interface CycleCountVarianceFilter {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  startDate?: string;
  endDate?: string;
  accountId?: string;
  materialId?: string;
}
