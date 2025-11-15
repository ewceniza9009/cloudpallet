import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../core/models/paged-result.dto';

export interface AddressDto {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CompanyDto {
  id: string;
  name: string;
  taxId: string;
  address: AddressDto;
  phoneNumber: string;
  email: string;
  website: string;
  status: string;
  subscriptionPlan: string;
}

export interface UpdateCompanyCommand {
  id: string;
  name: string;
  taxId: string;
  address: AddressDto;
  phoneNumber: string;
  email: string;
  website: string;
}

export interface WarehouseDto {
  id: string;
  companyId: string;
  name: string;
  address: AddressDto;
  operatingHours: string;
  contactPhone: string;
  contactEmail: string;
  isActive: boolean;
}

export interface CreateWarehouseCommand {
  name: string;
  address: AddressDto;
  operatingHours: string;
  contactPhone: string;
  contactEmail: string;
}

export interface UpdateWarehouseCommand {
  id: string;
  name: string;
  address: AddressDto;
  operatingHours: string;
  contactPhone: string;
  contactEmail: string;
  isActive: boolean;
}

export interface SupplierDto {
    id: string;
    name: string;
    phone: string;
    city: string;
    isActive: boolean;
}

export interface SupplierDetailDto {
    id: string;
    name: string;
    description: string;
    address: AddressDto;
    contactName: string;
    phone: string;
    email: string;
    taxId: string;
    leadTimeDays: number;
    certificationColdChain: boolean;
    paymentTerms: string;
    currencyCode: string;
    isActive: boolean;
    creditLimit: number;
}

export interface CreateSupplierCommand {
    name: string;
    description: string;
    address: AddressDto;
    contactName: string;
    phone: string;
    email: string;
    taxId: string;
    leadTimeDays: number;
    certificationColdChain: boolean;
    paymentTerms: string;
    currencyCode: string;
    creditLimit: number;
}

export interface GetSuppliersQuery {
    page: number;
    pageSize: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
    searchTerm?: string;
}

export interface UpdateSupplierCommand extends SupplierDetailDto {

}

export type LocationType = 'Picking' | 'Storage' | 'Staging';
export type ServiceType =
  | 'Storage'
  | 'Handling'
  | 'VAS'
  | 'Staging'
  | 'Chilling'
  | 'FrozenStorage'
  | 'CoolStorage'
  | 'DeepFrozenStorage'
  | 'ULTStorage'
  | 'Blasting'
  | 'Repack'
  | 'Split'
  | 'Labeling'
  | 'CrossDock'
  | 'Fumigation'
  | 'Surcharge'
  | 'CycleCount'
  | 'Kitting';

export interface RoomDto {
  id: string;
  name: string;
  serviceType: string;
  minTemp: number;
  maxTemp: number;
  locationCount: number;
}

export interface RoomDetailDto {
  id: string;
  warehouseId: string;
  name: string;
  serviceType: string;
  minTemp: number;
  maxTemp: number;
  isActive: boolean;
}

export interface LocationDto {
  id: string;
  barcode: string;
  bay: string;
  row: number;
  column: number;
  level: number;
  zoneType: LocationType;
  capacityWeight: number;
  isEmpty: boolean;
  isActive: boolean;
}

export interface CreateRoomCommand {
  warehouseId: string;
  name: string;
  minTemp: number;
  maxTemp: number;
  serviceType: ServiceType;
}

export interface CreateLocationsInBayCommand {
  roomId: string;
  bay: string;
  startRow: number;
  endRow: number;
  startCol: number;
  endCol: number;
  startLevel: number;
  endLevel: number;
  zoneType: 'Storage';
}

export interface CreateSimpleLocationCommand {
  roomId: string;
  name: string;
  zoneType: 'Staging' | 'Picking';
}

export interface UpdateLocationCommand {
  locationId: string;
  zoneType: LocationType;
  capacityWeight: number;
  isActive: boolean;
}

export interface GetLocationsForRoomQuery {
  roomId: string;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  searchTerm?: string;
}

export interface CarrierDto {
  id: string;
  name: string;
  scacCode: string;
  contactName: string;
  contactPhone: string;
  address: AddressDto;
  isActive: boolean;
  truckCount: number;
}

export interface CarrierDetailDto {
  id: string;
  name: string;
  scacCode: string;
  contactName: string;
  contactPhone: string;
  contactEmail: string;
  address: AddressDto;
  certificationColdChain: boolean;
  insurancePolicyNumber: string;
  insuranceExpiryDate: string | null;
  isActive: boolean;
}

export interface TruckDto {
  id: string;
  carrierId: string;
  licensePlate: string;
  model: string;
  capacityWeight: number;
  capacityVolume: number;
  isActive: boolean;
}

export interface CreateCarrierCommand {
  name: string;
  scacCode: string;
  contactName?: string;
  contactPhone?: string;
  contactEmail?: string;
  address?: AddressDto;
  certificationColdChain?: boolean;
  insurancePolicyNumber?: string;
  insuranceExpiryDate?: string;
}

export interface UpdateCarrierCommand extends CarrierDetailDto {}

export interface CreateTruckCommand {
  carrierId: string;
  licensePlate: string;
  model: string;
  capacityWeight: number;
  capacityVolume: number;
}

export interface UpdateTruckCommand extends TruckDto {}

export interface DockSetupDto {
    id: string;
    name: string;
    type: 'Inbound' | 'Outbound' | 'Both';
}
export interface YardSpotSetupDto {
    id: string;
    spotNumber: string;
    isActive: boolean;
}
export interface DockYardSetupDto {
    docks: DockSetupDto[];
    yardSpots: YardSpotSetupDto[];
}
export interface CreateDockCommand {
    warehouseId: string;
    name: string;
}
export interface UpdateDockCommand {
    dockId: string;
    name: string;
}
export interface CreateYardSpotCommand {
    warehouseId: string;
    spotNumber: string;
}
export interface UpdateYardSpotCommand {
    yardSpotId: string;
    spotNumber: string;
    isActive: boolean;
}

export interface AccountDto {
    id: string;
    name: string;
    phone: string;
    city: string;
    isActive: boolean;
}

export interface AccountDetailDto {
    id: string;
    name: string;
    typeId: 'ThreePL' | 'Direct' | 'Vendor';
    categoryId: string | null;
    address: AddressDto;
    contactName: string;
    phone: string;
    email: string;
    taxId: string;
    leadTimeDays: number;
    certificationColdChain: boolean;
    paymentTerms: string;
    currencyCode: string;
    preferredTempZone: 'Chilling' | 'FrozenStorage' | 'CoolStorage' | 'DeepFrozenStorage' | 'ULTStorage' | null;
    isPreferred: boolean;
    isActive: boolean;
    creditLimit: number;
}

export interface CreateAccountCommand {
    name: string;
    typeId: 'ThreePL' | 'Direct' | 'Vendor';
    categoryId: string | null;
    address: AddressDto;
    contactName: string;
    phone: string;
    email: string;
    taxId: string;
    leadTimeDays: number;
    certificationColdChain: boolean;
    paymentTerms: string;
    currencyCode: string;
    preferredTempZone: 'Chilling' | 'FrozenStorage' | 'CoolStorage' | 'DeepFrozenStorage' | 'ULTStorage' | null;
    isPreferred: boolean;
    creditLimit: number;
}

export interface UpdateAccountCommand extends AccountDetailDto {

}

export interface GetAccountsQuery {
    page: number;
    pageSize: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
    searchTerm?: string;
}

export interface UnitOfMeasureDto {
    id: string;
    name: string;
    symbol: string;
}

export interface UnitOfMeasureDetailDto {
    id: string;
    name: string;
    symbol: string;
}

export interface GetUnitOfMeasuresQuery {
    page: number;
    pageSize: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
    searchTerm?: string;
}

export interface CreateUnitOfMeasureCommand {
    name: string;
    symbol: string;
}

export interface UpdateUnitOfMeasureCommand {
    id: string;
    name: string;
    symbol: string;
}

export interface PalletTypeDto {
    id: string;
    name: string;
    tareWeight: number;
    length: number;
    width: number;
    height: number;
    isActive: boolean;
}

export interface PalletTypeDetailDto extends PalletTypeDto {}

export interface GetPalletTypesQuery {
    page: number;
    pageSize: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
    searchTerm?: string;
}

export interface CreatePalletTypeCommand {
    name: string;
    tareWeight: number;
    length: number;
    width: number;
    height: number;
}

export interface UpdatePalletTypeCommand {
    id: string;
    name: string;
    tareWeight: number;
    length: number;
    width: number;
    height: number;
    isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminSetupApiService {
  private http = inject(HttpClient);
  private companyApiUrl = `${environment.apiUrl}/Company`;
  private warehouseApiUrl = `${environment.apiUrl}/setup/warehouses`;

  private roomsApiUrl = `${environment.apiUrl}/setup/rooms`;
  private locationsApiUrl = `${environment.apiUrl}/setup/locations`;

  private carriersApiUrl = `${environment.apiUrl}/setup/carriers`;
  private trucksApiUrl = `${environment.apiUrl}/setup/trucks`;

  private dockYardApiUrl = `${environment.apiUrl}/setup`;

  private suppliersApiUrl = `${environment.apiUrl}/setup/suppliers`;
  private accountsApiUrl = `${environment.apiUrl}/setup/accounts`;

  private uomApiUrl = `${environment.apiUrl}/setup/unit-of-measures`;
  private palletTypeApiUrl = `${environment.apiUrl}/setup/pallet-types`;

  getCompanyDetails(): Observable<CompanyDto> {
    return this.http.get<CompanyDto>(this.companyApiUrl);
  }
  updateCompanyDetails(command: UpdateCompanyCommand): Observable<void> {
    return this.http.put<void>(this.companyApiUrl, command);
  }

  getWarehouses(): Observable<WarehouseDto[]> {
    return this.http.get<WarehouseDto[]>(this.warehouseApiUrl);
  }
  getWarehouseById(id: string): Observable<WarehouseDto> {
    return this.http.get<WarehouseDto>(`${this.warehouseApiUrl}/${id}`);
  }
  createWarehouse(command: CreateWarehouseCommand): Observable<string> {
    return this.http.post<string>(this.warehouseApiUrl, command);
  }
  updateWarehouse(
    id: string,
    command: UpdateWarehouseCommand
  ): Observable<void> {
    return this.http.put<void>(`${this.warehouseApiUrl}/${id}`, command);
  }
  deleteWarehouse(id: string): Observable<void> {
    return this.http.delete<void>(`${this.warehouseApiUrl}/${id}`);
  }

  getAllRooms(): Observable<RoomDto[]> {
    return this.http.get<RoomDto[]>(this.roomsApiUrl);
  }

  getRoomById(id: string): Observable<RoomDetailDto> {
    return this.http.get<RoomDetailDto>(`${this.roomsApiUrl}/${id}`);
  }

  createRoom(command: CreateRoomCommand): Observable<string> {
    return this.http.post<string>(this.roomsApiUrl, command);
  }

  getLocationsForRoom(
    query: GetLocationsForRoomQuery
  ): Observable<PagedResult<LocationDto>> {
    let params = new HttpParams()
      .set('page', query.page.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection)
      params = params.set('sortDirection', query.sortDirection);
    if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);

    return this.http.get<PagedResult<LocationDto>>(
      `${this.roomsApiUrl}/${query.roomId}/locations`,
      { params }
    );
  }

  createLocationsInBay(command: CreateLocationsInBayCommand): Observable<void> {
    return this.http.post<void>(
      `${this.roomsApiUrl}/${command.roomId}/locations-bay`,
      command
    );
  }

  createSimpleLocation(
    command: CreateSimpleLocationCommand
  ): Observable<string> {
    return this.http.post<string>(
      `${this.roomsApiUrl}/${command.roomId}/locations-simple`,
      command
    );
  }

  updateLocation(id: string, command: UpdateLocationCommand): Observable<void> {
    return this.http.put<void>(`${this.locationsApiUrl}/${id}`, command);
  }

  deleteLocation(id: string): Observable<void> {
    return this.http.delete<void>(`${this.locationsApiUrl}/${id}`);
  }

  getAllCarriers(): Observable<CarrierDto[]> {
    return this.http.get<CarrierDto[]>(this.carriersApiUrl);
  }

  getCarrierById(id: string): Observable<CarrierDetailDto> {
    return this.http.get<CarrierDetailDto>(`${this.carriersApiUrl}/${id}`);
  }

  createCarrier(command: CreateCarrierCommand): Observable<string> {
    return this.http.post<string>(this.carriersApiUrl, command);
  }

  updateCarrier(id: string, command: UpdateCarrierCommand): Observable<void> {
    return this.http.put<void>(`${this.carriersApiUrl}/${id}`, command);
  }

  deleteCarrier(id: string): Observable<void> {
    return this.http.delete<void>(`${this.carriersApiUrl}/${id}`);
  }

  getTrucksByCarrier(carrierId: string): Observable<TruckDto[]> {
    return this.http.get<TruckDto[]>(
      `${this.carriersApiUrl}/${carrierId}/trucks`
    );
  }

  createTruck(command: CreateTruckCommand): Observable<string> {
    return this.http.post<string>(
      `${this.carriersApiUrl}/${command.carrierId}/trucks`,
      command
    );
  }

  updateTruck(id: string, command: UpdateTruckCommand): Observable<void> {
    return this.http.put<void>(`${this.trucksApiUrl}/${id}`, command);
  }

  deleteTruck(id: string): Observable<void> {
    return this.http.delete<void>(`${this.trucksApiUrl}/${id}`);
  }

  getDockYardSetup(warehouseId: string): Observable<DockYardSetupDto> {
    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<DockYardSetupDto>(`${this.dockYardApiUrl}/dock-yard`, { params });
  }


  createDock(command: CreateDockCommand): Observable<string> {
    return this.http.post<string>(`${this.dockYardApiUrl}/docks`, command);
  }
  updateDock(id: string, command: UpdateDockCommand): Observable<void> {
    return this.http.put<void>(`${this.dockYardApiUrl}/docks/${id}`, command);
  }
  deleteDock(id: string): Observable<void> {
    return this.http.delete<void>(`${this.dockYardApiUrl}/docks/${id}`);
  }


  createYardSpot(command: CreateYardSpotCommand): Observable<string> {
    return this.http.post<string>(`${this.dockYardApiUrl}/yard-spots`, command);
  }
  updateYardSpot(id: string, command: UpdateYardSpotCommand): Observable<void> {
    return this.http.put<void>(`${this.dockYardApiUrl}/yard-spots/${id}`, command);
  }
  deleteYardSpot(id: string): Observable<void> {
    return this.http.delete<void>(`${this.dockYardApiUrl}/yard-spots/${id}`);
  }

  getAllSuppliers(query: GetSuppliersQuery): Observable<PagedResult<SupplierDto>> {
        let params = new HttpParams()
            .set('page', query.page.toString())
            .set('pageSize', query.pageSize.toString());

        if (query.sortBy) params = params.set('sortBy', query.sortBy);
        if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
        if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);

        return this.http.get<PagedResult<SupplierDto>>(this.suppliersApiUrl, { params });
    }

  getSupplierById(id: string): Observable<SupplierDetailDto> {
    return this.http.get<SupplierDetailDto>(`${this.suppliersApiUrl}/${id}`);
  }

  createSupplier(command: CreateSupplierCommand): Observable<string> {
    return this.http.post<string>(this.suppliersApiUrl, command);
  }

  updateSupplier(id: string, command: UpdateSupplierCommand): Observable<void> {
    return this.http.put<void>(`${this.suppliersApiUrl}/${id}`, command);
  }

  deleteSupplier(id: string): Observable<void> {
    return this.http.delete<void>(`${this.suppliersApiUrl}/${id}`);
  }

  getPagedAccounts(query: GetAccountsQuery): Observable<PagedResult<AccountDto>> {
    let params = new HttpParams()
        .set('page', query.page.toString())
        .set('pageSize', query.pageSize.toString());

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);

    return this.http.get<PagedResult<AccountDto>>(this.accountsApiUrl, { params });
  }

  getAccountById(id: string): Observable<AccountDetailDto> {
    return this.http.get<AccountDetailDto>(`${this.accountsApiUrl}/${id}`);
  }

  createAccount(command: CreateAccountCommand): Observable<string> {
    return this.http.post<string>(this.accountsApiUrl, command);
  }

  updateAccount(id: string, command: UpdateAccountCommand): Observable<void> {
    return this.http.put<void>(`${this.accountsApiUrl}/${id}`, command);
  }

  deleteAccount(id: string): Observable<void> {
    return this.http.delete<void>(`${this.accountsApiUrl}/${id}`);
  }

  getPagedUoMs(query: GetUnitOfMeasuresQuery): Observable<PagedResult<UnitOfMeasureDto>> {
    let params = new HttpParams()
        .set('page', query.page.toString())
        .set('pageSize', query.pageSize.toString());

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);

    return this.http.get<PagedResult<UnitOfMeasureDto>>(this.uomApiUrl, { params });
  }

  getUoMById(id: string): Observable<UnitOfMeasureDetailDto> {
    return this.http.get<UnitOfMeasureDetailDto>(`${this.uomApiUrl}/${id}`);
  }

  createUoM(command: CreateUnitOfMeasureCommand): Observable<string> {
    return this.http.post<string>(this.uomApiUrl, command);
  }

  updateUoM(id: string, command: UpdateUnitOfMeasureCommand): Observable<void> {
    return this.http.put<void>(`${this.uomApiUrl}/${id}`, command);
  }

  deleteUoM(id: string): Observable<void> {
    return this.http.delete<void>(`${this.uomApiUrl}/${id}`);
  }

  getPagedPalletTypes(query: GetPalletTypesQuery): Observable<PagedResult<PalletTypeDto>> {
    let params = new HttpParams()
        .set('page', query.page.toString())
        .set('pageSize', query.pageSize.toString());

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);

    return this.http.get<PagedResult<PalletTypeDto>>(this.palletTypeApiUrl, { params });
  }

  getPalletTypeById(id: string): Observable<PalletTypeDetailDto> {
    return this.http.get<PalletTypeDetailDto>(`${this.palletTypeApiUrl}/${id}`);
  }

  createPalletType(command: CreatePalletTypeCommand): Observable<string> {
    return this.http.post<string>(this.palletTypeApiUrl, command);
  }

  updatePalletType(id: string, command: UpdatePalletTypeCommand): Observable<void> {
    return this.http.put<void>(`${this.palletTypeApiUrl}/${id}`, command);
  }

  deletePalletType(id: string): Observable<void> {
    return this.http.delete<void>(`${this.palletTypeApiUrl}/${id}`);
  }
}
