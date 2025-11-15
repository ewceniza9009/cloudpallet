import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WarehouseStateService } from '../../core/services/warehouse-state.service';

export interface ShippableGroupDto {
  accountId: string;
  accountName: string;
  itemCount: number;
  totalQuantity: number;
  pickTransactionIds: string[];
}

export interface ShipGoodsCommand {
  appointmentId: string;
  shipmentNumber: string;
  pickTransactionIds: string[];
}

@Injectable({ providedIn: 'root' })
export class ShippingApiService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Shipping`;
  private warehouseState = inject(WarehouseStateService);

  getShippableGroups(): Observable<ShippableGroupDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      throw new Error('Cannot get shippable groups: No warehouse is selected.');
    }
    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<ShippableGroupDto[]>(`${this.apiUrl}/ready-to-ship`, {
      params,
    });
  }

  shipGoods(command: ShipGoodsCommand): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/ship`, command);
  }
}
