import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WarehouseStateService } from '../../core/services/warehouse-state.service';

export interface YardAppointmentDto {
  appointmentId: string;
  truckId: string;
  carrierName: string;
  licensePlate: string;
  appointmentTime: string;
  status: string;
  dockName: string;
}

export interface TruckCheckInCommand {
  truckId: string;
  yardSpotId: string;
}

export interface OccupiedYardSpotDto {
  yardSpotId: string;
  spotNumber: string;
  licensePlate: string;
  truckId: string;
  appointmentId: string;
  occupiedSince: string;
}

export interface YardSpotDto {
  id: string;
  spotNumber: string;
}

@Injectable({ providedIn: 'root' })
export class YardApiService {
  private http = inject(HttpClient);
  private warehouseState = inject(WarehouseStateService);
  private apiUrl = `${environment.apiUrl}/Yard`;

  getTodaysAppointments(
    startDate: string,
    endDate: string
  ): Observable<YardAppointmentDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) throw new Error('Warehouse not selected');

    const params = new HttpParams()
      .set('warehouseId', warehouseId)
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<YardAppointmentDto[]>(
      `${this.apiUrl}/appointments/today`,
      { params }
    );
  }

  checkInTruck(truckId: string, yardSpotId: string): Observable<string> {
    const command: TruckCheckInCommand = { truckId, yardSpotId };
    return this.http.post<string>(`${this.apiUrl}/check-in`, command);
  }

  getAvailableYardSpots(): Observable<YardSpotDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) throw new Error('Warehouse not selected');
    const params = new HttpParams().set('warehouseId', warehouseId);

    return this.http.get<YardSpotDto[]>(
      `${environment.apiUrl}/Lookups/available-yard-spots`,
      { params }
    );
  }

  getOccupiedYardSpots(): Observable<OccupiedYardSpotDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) throw new Error('Warehouse not selected');

    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<OccupiedYardSpotDto[]>(
      `${this.apiUrl}/occupied-spots`,
      { params }
    );
  }

  moveTruckToDock(yardSpotId: string, appointmentId: string): Observable<void> {
    const command = { yardSpotId, appointmentId };
    return this.http.post<void>(`${this.apiUrl}/move-to-dock`, command);
  }

  vacateYardSpot(yardSpotId: string): Observable<void> {
    const command = { yardSpotId };
    return this.http.post<void>(`${this.apiUrl}/vacate-spot`, command);
  }
}
