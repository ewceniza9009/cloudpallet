import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LocationDto {
  id: string;
  barcode: string;
  row: number;
  column: number;
  level: number;
  currentWeight: number;
  capacityWeight: number;
  utilization: number;
  status: string;
}

export interface BayDto {
  bayName: string;
  currentWeight: number;
  capacityWeight: number;
  utilization: number;
  status: string;
  locations: LocationDto[];
}

export interface RoomDto {
  roomName: string;
  currentWeight: number;
  capacityWeight: number;
  utilization: number;
  bays: BayDto[];
}

export interface LocationOverviewDto {
  rooms: RoomDto[];
}

@Injectable({ providedIn: 'root' })
export class WarehouseApiService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Warehouse`;

  getLocationOverview(warehouseId: string): Observable<LocationOverviewDto> {
    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<LocationOverviewDto>(`${this.apiUrl}/overview`, {
      params,
    });
  }
}
