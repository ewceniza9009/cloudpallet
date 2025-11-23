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
  gs1CompanyPrefix: string;
  defaultBarcodeFormat: string;
}

export interface UpdateCompanyCommand {
  name: string;
  taxId: string;
  address: AddressDto;
  phoneNumber: string;
  email: string;
  website: string;
  gs1CompanyPrefix: string;
  defaultBarcodeFormat: string;
}

export type UserRole = 'Admin' | 'Operator' | 'Finance';

export interface UserDto {
  id: string;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
}

export type ServiceType =
  | 'Storage'
  | 'Blasting'
  | 'Handling'
  | 'VAS'
  | 'FrozenStorage'
  | 'Chilling'
  | 'Staging'
  | 'CoolStorage'
  | 'DeepFrozenStorage'
  | 'ULTStorage'
  | 'Repack'
  | 'Split'
  | 'Labeling'
  | 'CrossDock'
  | 'Fumigation'
  | 'Surcharge'
  | 'CycleCount'
  | 'Kitting';

export type RateUom =
  | 'Pallet'
  | 'Kg'
  | 'Day'
  | 'Cycle'
  | 'Each'
  | 'Hour'
  | 'Shipment'
  | 'Percent';

export type MaterialType = 'Normal' | 'Kit' | 'Repack';

export type BarcodeFormat = 'GS1_128' | 'UPC';

export interface Rate {
  id: string;
  accountId: string | null;
  serviceType: ServiceType;
  uom: RateUom;
  value: number;
  tier: string;
  effectiveStartDate: string;
  effectiveEndDate: string | null;
  isActive: boolean;
}

export interface RateDto {
  id: string;
  accountId: string | null;
  serviceType: ServiceType;
  uom: RateUom;
  value: number;
  tier: string;
  effectiveStartDate: string;
  effectiveEndDate: string | null;
  isActive: boolean;
}

export interface CreateRateCommand {
  accountId: string | null;
  serviceType: ServiceType;
  uom: RateUom;
  value: number;
  tier: string;
  effectiveStartDate: string;
  effectiveEndDate: string | null;
}

export interface UpdateRateCommand {
  id: string;
  accountId: string | null;
  serviceType: ServiceType;
  uom: RateUom;
  value: number;
  tier: string;
  effectiveStartDate: string;
  effectiveEndDate: string | null;
}

export interface MaterialDetailDto {
  id: string;
  name: string;
  sku: string;
  description: string;
  categoryId: string;
  uomId: string;
  requiredTempZone:
    | 'Chilling'
    | 'FrozenStorage'
    | 'CoolStorage'
    | 'DeepFrozenStorage'
    | 'ULTStorage';
  baseWeight: number;
  costPerUnit: number;
  materialType: MaterialType;
  perishable: boolean;
  shelfLifeDays: number;
  isHazardous: boolean;
  gs1BarcodePrefix: string;
  isActive: boolean;

  defaultBarcodeFormat: BarcodeFormat;
  dimensionsLength: number;
  dimensionsWidth: number;
  dimensionsHeight: number;
  minStockLevel: number;
  maxStockLevel: number;
  packageTareWeightPerUom: number;
}

export interface CreateMaterialCommand {
  name: string;
  sku: string;
  description: string;
  categoryId: string;
  uomId: string;
  requiredTempZone:
    | 'Chilling'
    | 'FrozenStorage'
    | 'CoolStorage'
    | 'DeepFrozenStorage'
    | 'ULTStorage';
  baseWeight: number;
  costPerUnit: number;
  materialType: MaterialType;

  perishable?: boolean;
  shelfLifeDays?: number;
  isHazardous?: boolean;
  gs1BarcodePrefix?: string;
  defaultBarcodeFormat?: BarcodeFormat;
  dimensionsLength?: number;
  dimensionsWidth?: number;
  dimensionsHeight?: number;
  minStockLevel?: number;
  maxStockLevel?: number;
  packageTareWeightPerUom?: number;
}

export interface UpdateMaterialCommand {
  id: string;
  name: string;
  description: string;
  costPerUnit: number;
  categoryId: string;
  requiredTempZone:
    | 'Chilling'
    | 'FrozenStorage'
    | 'CoolStorage'
    | 'DeepFrozenStorage'
    | 'ULTStorage';
  baseWeight: number;
  materialType: MaterialType;
  perishable: boolean;
  shelfLifeDays: number;
  isHazardous: boolean;
  gs1BarcodePrefix: string;
  isActive: boolean;

  defaultBarcodeFormat: BarcodeFormat;
  dimensionsLength: number;
  dimensionsWidth: number;
  dimensionsHeight: number;
  minStockLevel: number;
  maxStockLevel: number;
  packageTareWeightPerUom: number;
}

export interface BomLineDto {
  inputMaterialId: string;
  inputQuantity: number;
}

export interface CreateBillOfMaterialCommand {
  outputMaterialId: string;
  outputQuantity: number;
  lines: BomLineDto[];
}

export interface BomDto {
  id: string;
  outputMaterialId: string;
  outputQuantity: number;
  lines: BomLineDto[];
}

export interface GetMaterialsQuery {
    page: number;
    pageSize: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
    searchTerm?: string;
}

export interface GetRatesQuery {
    page: number;
    pageSize: number;
    sortBy?: string;
    sortDirection?: 'asc' | 'desc';
    searchTerm?: string;
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private http = inject(HttpClient);

  private adminApiUrl = `${environment.apiUrl}/Admin`;
  private companyApiUrl = `${environment.apiUrl}/Company`;

  getCompanyDetails(): Observable<CompanyDto> {
    return this.http.get<CompanyDto>(this.companyApiUrl);
  }

  updateCompanyDetails(command: UpdateCompanyCommand): Observable<void> {
    return this.http.put<void>(this.companyApiUrl, command);
  }

  getUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${this.adminApiUrl}/users`);
  }

  updateUserRole(userId: string, newRole: UserRole): Observable<void> {
    return this.http.put<void>(
      `${this.adminApiUrl}/users/${userId}/role`,
      `"${newRole}"`,
      {
        headers: { 'Content-Type': 'application/json' },
      }
    );
  }

  getRates(query: GetRatesQuery): Observable<PagedResult<RateDto>> {
    let params = new HttpParams()
        .set('page', query.page.toString())
        .set('pageSize', query.pageSize.toString());

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);

    return this.http.get<PagedResult<RateDto>>(`${this.adminApiUrl}/rates`, { params });
  }

  createRate(command: CreateRateCommand): Observable<string> {
    return this.http.post<string>(`${this.adminApiUrl}/rates`, command);
  }

  getRateById(id: string): Observable<RateDto> {
    return this.http.get<RateDto>(`${this.adminApiUrl}/rates/${id}`);
  }

  updateRate(id: string, command: UpdateRateCommand): Observable<void> {
    return this.http.put<void>(`${this.adminApiUrl}/rates/${id}`, command);
  }

  deleteRate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.adminApiUrl}/rates/${id}`);
  }

  getMaterials(): Observable<MaterialDetailDto[]> {
    return this.http.get<MaterialDetailDto[]>(`${this.adminApiUrl}/materials/lookup`);
  }

  getPagedMaterials(query: GetMaterialsQuery): Observable<PagedResult<MaterialDetailDto>> {
     let params = new HttpParams()
        .set('page', query.page.toString())
        .set('pageSize', query.pageSize.toString());

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.searchTerm) params = params.set('searchTerm', query.searchTerm);

    return this.http.get<PagedResult<MaterialDetailDto>>(`${this.adminApiUrl}/materials`, { params });
  }

  createMaterial(command: CreateMaterialCommand): Observable<string> {
    return this.http.post<string>(`${this.adminApiUrl}/materials`, command);
  }

  getMaterialById(id: string): Observable<MaterialDetailDto> {
    return this.http.get<MaterialDetailDto>(
      `${this.adminApiUrl}/materials/${id}`
    );
  }

  updateMaterial(id: string, command: UpdateMaterialCommand): Observable<void> {
    return this.http.put<void>(`${this.adminApiUrl}/materials/${id}`, command);
  }

  deleteMaterial(id: string): Observable<void> {
    return this.http.delete<void>(`${this.adminApiUrl}/materials/${id}`);
  }

  createBillOfMaterial(
    command: CreateBillOfMaterialCommand
  ): Observable<string> {
    return this.http.post<string>(`${this.adminApiUrl}/boms`, command);
  }

  getBomForMaterial(outputMaterialId: string): Observable<BomDto> {
    return this.http.get<BomDto>(
      `${this.adminApiUrl}/materials/${outputMaterialId}/bom`
    );
  }
}
