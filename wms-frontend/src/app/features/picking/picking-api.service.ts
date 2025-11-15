import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PickItem {
  pickId: string;
  material: string;
  sku: string;
  location: string;
  quantity: number;
  status: 'Planned' | 'Confirmed' | 'Short';
}

export interface PickListGroupDto {
  accountId: string;
  accountName: string;
  items: PickItem[];
}

export interface ConfirmPickRequest {
  pickTransactionId: string;
  newStatus: 'Confirmed' | 'Short';
}

export interface ConfirmPickByScanRequest {
  pickTransactionId: string;
  scannedLocationCode: string;
  scannedLpn: string;
  actualWeight: number;
}

export interface OrderItemDto {
  materialId: string;
  quantity: number;
}

export interface CreatePickListRequest {
  orderItems: OrderItemDto[];
  isExpedited: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class PickingApiService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Picking`;

  getPickList(): Observable<PickListGroupDto[]> {
    return this.http.get<PickListGroupDto[]>(`${this.apiUrl}/lists`);
  }

  createPickList(request: CreatePickListRequest): Observable<string[]> {
    return this.http.post<string[]>(`${this.apiUrl}/create-list`, request);
  }

  confirmPickManually(request: ConfirmPickRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/confirm-manual`, request);
  }

  confirmPickByScan(request: ConfirmPickByScanRequest): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/confirm-by-scan`, request);
  }
}
