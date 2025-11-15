import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { DockAppointmentDto } from '../../core/models/dock-appointment.dto';
import { environment } from '../../../environments/environment';
import { WarehouseStateService } from '../../core/services/warehouse-state.service';

export interface DockDto {
  id: string;
  name: string;
  isAvailable: boolean;
}

export interface ScheduleAppointmentCommand {
  dockId: string;
  licensePlate: string;
  supplierId: string;
  accountId: string;
  startDateTime: string;
  endDateTime: string;
  type: 'Receiving' | 'Shipping';
}

export interface AppointmentDetailsDto {
  appointmentId: string;
  supplierId: string;
  accountId: string;
}

interface TruckDto {
  id: string;
  licensePlate: string;
}

@Injectable({ providedIn: 'root' })
export class DockApiService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  private warehouseState = inject(WarehouseStateService);

  getDocks(): Observable<DockDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      console.error('Cannot get docks: No warehouse is selected.');
      return of([]);
    }
    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<DockDto[]>(`${this.apiUrl}/Lookups/docks`, { params });
  }

  searchAppointmentsByPlate(
    licensePlate: string
  ): Observable<DockAppointmentDto[]> {
    const params = new HttpParams().set('licensePlate', licensePlate);
    return this.http.get<DockAppointmentDto[]>(
      `${this.apiUrl}/DockAppointments/search`,
      { params }
    );
  }

  getAppointmentsForDock(
    dockId: string,
    startDate: string,
    endDate: string
  ): Observable<DockAppointmentDto[]> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);
    return this.http.get<DockAppointmentDto[]>(
      `${this.apiUrl}/docks/${dockId}/appointments`,
      { params }
    );
  }

  scheduleAppointment(command: ScheduleAppointmentCommand): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/DockAppointments`, command);
  }

  getAppointmentDetails(
    appointmentId: string
  ): Observable<AppointmentDetailsDto> {
    return this.http.get<AppointmentDetailsDto>(
      `${this.apiUrl}/DockAppointments/${appointmentId}`
    );
  }
}
