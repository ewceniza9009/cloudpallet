import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WarehouseStateService } from '../../core/services/warehouse-state.service';

export type ComplianceLabelType = 'None' | 'Export' | 'Allergen' | 'ForeignLanguage';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

export interface WeightDto {
  value: number;
  unit: string;
}

export interface MaterialDto {
  id: string;
  name: string;
  sku: string;
  materialType: string;
}

export interface LocationDto {
  id: string;
  displayName: string;
}

export interface PalletTypeDto {
  id: string;
  name: string;
  tareWeight: number;
}

export interface CreateReceivingSessionCommand {
  supplierId: string;
  dockAppointmentId: string | null;
  accountId: string;
  remarks: string | null;
}

export interface AddPalletToReceivingCommand {
  receivingId: string;
  palletTypeId: string;
  isCrossDock: boolean;
}

export interface ProcessPalletLineCommand {
  receivingId: string;
  palletId: string;
  palletLineId: string;
  materialId: string;
  quantity: number;
  batchNumber: string;
  dateOfManufacture: string;
  expiryDate: string | null;
  grossWeight: number;
}

export interface ReceivingSessionDto {
  receivingId: string;
  supplierName: string;
  licensePlate: string | null;
  status: string;
  timestamp: string;
  palletCount: number;
}

export interface PalletLineDetailDto {
  palletLineId: string;
  materialId: string;
  materialName: string;
  netWeight: number;
  barcode: string;
  status: 'Pending' | 'Processed';
  quantity: number;
  batchNumber: string;
  dateOfManufacture: string;
  expiryDate?: string;
}

export interface PalletDetailDto {
  id: string;
  palletNumber: string;
  palletTypeName: string;
  tareWeight: number;
  lines: PalletLineDetailDto[];
}

export interface ReceivingSessionDetailDto {
  receivingId: string;
  supplierId: string;
  accountId: string;
  dockAppointmentId: string;
  pallets: PalletDetailDto[];
}

export interface PutawayTaskDto {
  palletId: string;
  palletBarcode: string;
  contents: string;
  currentLocation: string;
  suggestedLocationId: string;
  suggestedLocation: string;
}

export interface TransferPalletCommand {
  palletId: string;
  sourceLocationId: string;
  destinationLocationId: string;
}

export interface TransferItemsToNewPalletCommand {
  sourceInventoryId: string;
  quantityToMove: number;
  newPalletTypeId: string;
  weighedWeight: number | null;
}

export interface PalletMovementDto {
  eventType: string;
  timestamp: string;
  location: string;
  details: string;
}

export interface PalletLineItemDto {
  inventoryId: string;
  materialName: string;
  quantity: number;
  barcode: string;
}

export interface StoredPalletDetailDto {
  palletId: string;
  palletBarcode: string;
  currentLocationId: string;
  currentLocationBarcode: string;
  lines: PalletLineItemDto[];
}

export interface RoomWithPalletsDto {
  roomName: string;
  pallets: StoredPalletDetailDto[];
}

export interface StoredPalletSearchResultDto {
  palletId: string;
  palletBarcode: string;
  locationName: string;
  accountName: string;
  materialSummary: string;
  quantity: number;
}

export interface RepackableInventoryDto {
  inventoryId: string;
  materialId: string;
  materialName: string;
  sku: string;
  location: string;
  quantity: number;
  palletBarcode: string;
  batchNumber?: string;
}

export interface RecordVasCommand {
  palletId?: string;
  serviceType: 'Blasting' | 'Repack' | 'Labeling' | 'Fumigation' | 'CycleCount' | 'Split';

  sourceInventoryId?: string;
  targetMaterialId?: string;
  quantityToProcess?: number;

  targetId?: string;
  targetType?: 'Pallet' | 'InventoryItem';
  labelType?: ComplianceLabelType;
  quantityLabeled?: number;

  durationHours?: number;
  inventoryId?: string;

  countedItems?: { inventoryId: string; countedQuantity: number }[];
}

export interface RecordLabelingCommand {
  targetId: string;
  targetType: 'Pallet' | 'InventoryItem';
  labelType: ComplianceLabelType;
  quantityLabeled: number;
}
export interface StartQuarantineCommand {
  inventoryId: string;
  reason: string;
}
export interface CompleteFumigationCommand {
  inventoryId: string;
  durationHours: number;
}
export interface RecordCycleCountCommand {
  countedItems: { inventoryId: string; countedQuantity: number }[];
  durationHours: number;
}

// VAS Amendment DTOs
export interface VasTransactionDto {
  id: string;
  serviceType: string;
  timestamp: string;
  description: string;
  userId: string;
  userName: string;
  isAmended: boolean;
  isVoided: boolean;
  voidedAt: string | null;
  voidReason: string | null;
}

export interface VasTransactionLineDto {
  id: string;
  materialId: string | null;
  materialName: string | null;
  quantity: number;
  weight: number;
  isInput: boolean;
  isAmended: boolean;
  originalQuantity: number | null;
  originalWeight: number | null;
  amendedAt: string | null;
}

export interface VasAmendmentDto {
  id: string;
  timestamp: string;
  userName: string;
  reason: string;
  amendmentDetails: string;
  amendmentType: string;
}

export interface VasTransactionDetailDto {
  id: string;
  serviceType: string;
  timestamp: string;
  description: string;
  status: string;
  userName: string;
  isVoided: boolean;
  voidedAt: string | null;
  voidReason: string | null;
  inputLines: VasTransactionLineDto[];
  outputLines: VasTransactionLineDto[];
  amendmentHistory: VasAmendmentDto[];
}

export interface KitComponentDto {
  componentMaterialId: string;
  sourceInventoryId: string;
  quantityToConsume: number;
}
export interface CreateKitCommand {
  targetKitMaterialId: string;
  quantityToBuild: number;
  durationHours: number;
  components: KitComponentDto[];
}

export interface ReceivingVarianceLineDto {
  materialId: string;
  materialName: string;
  expectedQuantity: number;
  receivedQuantity: number;
  variance: number;
  status: string;
}

export interface ReceivingVarianceDto {
  receivingId: string;
  manifestId: string | null;
  lines: ReceivingVarianceLineDto[];
}

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  private inventoryUrl = `${this.apiUrl}/Inventory`;
  private lookupsUrl = `${this.apiUrl}/Lookups`;
  private receivingUrl = `${this.apiUrl}/Receiving`;
  private warehouseState = inject(WarehouseStateService);

  searchMaterials(
    searchTerm: string | null,
    page: number,
    pageSize: number
  ): Observable<PagedResult<MaterialDto>> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PagedResult<MaterialDto>>(`${this.lookupsUrl}/materials/search`, {
      params,
    });
  }

  getPalletTypes(): Observable<PalletTypeDto[]> {
    return this.http.get<PalletTypeDto[]>(`${this.lookupsUrl}/pallet-types`);
  }

  getAvailableStorageLocations(): Observable<LocationDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      throw new Error('Cannot get locations: No warehouse is selected.');
    }
    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<LocationDto[]>(`${this.lookupsUrl}/available-storage-locations`, {
      params,
    });
  }

  getRepackableInventory(
    accountId: string,
    materialId?: string,
    searchTerm?: string,
    page: number = 1,
    pageSize: number = 20
  ): Observable<PagedResult<RepackableInventoryDto>> {
    let params = new HttpParams()
      .set('accountId', accountId)
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (materialId) {
      params = params.set('materialId', materialId);
    }

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PagedResult<RepackableInventoryDto>>(`${this.lookupsUrl}/repackable-inventory`, {
      params,
    });
  }

  getReceivingSessions(
    page: number = 1,
    pageSize: number = 10,
    searchTerm?: string,
    date?: Date | null
  ): Observable<PagedResult<ReceivingSessionDto>> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      throw new Error('Cannot get receiving sessions: No warehouse is selected.');
    }

    let params = new HttpParams()
      .set('warehouseId', warehouseId)
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    if (date) {
      // Format as YYYY-MM-DD to avoid timezone issues in query params
      const dateString = date.getFullYear() + '-' + 
        ('0' + (date.getMonth() + 1)).slice(-2) + '-' + 
        ('0' + date.getDate()).slice(-2);
      params = params.set('date', dateString);
    }

    return this.http.get<PagedResult<ReceivingSessionDto>>(`${this.receivingUrl}/sessions`, {
      params,
    });
  }
  getReceivingSessionById(id: string): Observable<ReceivingSessionDetailDto> {
    return this.http.get<ReceivingSessionDetailDto>(`${this.receivingUrl}/session/${id}`);
  }
  createReceivingSession(command: CreateReceivingSessionCommand): Observable<string> {
    return this.http.post<string>(`${this.receivingUrl}/session`, command);
  }
  addPalletToReceiving(command: AddPalletToReceivingCommand): Observable<string> {
    return this.http.post<string>(
      `${this.receivingUrl}/session/${command.receivingId}/pallet`,
      command
    );
  }
  processPalletLine(command: ProcessPalletLineCommand): Observable<string> {
    return this.http.post(`${this.receivingUrl}/process-line`, command, {
      responseType: 'text',
    });
  }
  addLineToPallet(receivingId: string, palletId: string, materialId: string): Observable<void> {
    const body = { materialId: materialId };
    return this.http.post<void>(
      `${this.receivingUrl}/session/${receivingId}/pallet/${palletId}/line`,
      body
    );
  }
  deletePalletLine(receivingId: string, palletId: string, palletLineId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.receivingUrl}/session/${receivingId}/pallet/${palletId}/line/${palletLineId}`
    );
  }
  deletePallet(receivingId: string, palletId: string): Observable<void> {
    return this.http.delete<void>(`${this.receivingUrl}/session/${receivingId}/pallet/${palletId}`);
  }

// ... existing code ...

  completeReceivingSession(receivingId: string): Observable<void> {
    return this.http.post<void>(`${this.receivingUrl}/session/${receivingId}/complete`, {});
  }

  getReceivingVariance(receivingId: string): Observable<ReceivingVarianceDto> {
    return this.http.get<ReceivingVarianceDto>(`${this.receivingUrl}/session/${receivingId}/variance`);
  }

  // Delete a receiving session by ID
  deleteReceivingSession(receivingId: string): Observable<void> {
    return this.http.delete<void>(`${this.receivingUrl}/session/${receivingId}`);
  }

  getStoredPalletsByRoom(): Observable<RoomWithPalletsDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      throw new Error('Cannot get stored pallets: No warehouse is selected.');
    }
    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<RoomWithPalletsDto[]>(`${this.inventoryUrl}/stored-pallets-by-room`, {
      params,
    });
  }

  searchStoredPallets(
    accountId?: string,
    materialId?: string,
    barcodeQuery?: string
  ): Observable<StoredPalletSearchResultDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      throw new Error('No warehouse selected.');
    }

    let params = new HttpParams().set('warehouseId', warehouseId);
    if (accountId) {
      params = params.set('accountId', accountId);
    }
    if (materialId) {
      params = params.set('materialId', materialId);
    }
    if (barcodeQuery) {
      params = params.set('barcodeQuery', barcodeQuery);
    }

    return this.http.get<StoredPalletSearchResultDto[]>(
      `${this.inventoryUrl}/search-stored-pallets`,
      { params }
    );
  }

  getPalletHistory(palletBarcode: string): Observable<PalletMovementDto[]> {
    return this.http.get<PalletMovementDto[]>(
      `${this.inventoryUrl}/pallet-history/${palletBarcode}`
    );
  }

  getPutawayTasks(): Observable<PutawayTaskDto[]> {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      throw new Error('Cannot get putaway tasks: No warehouse is selected.');
    }
    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<PutawayTaskDto[]>(`${this.inventoryUrl}/putaway-tasks`, { params });
  }

  executePutaway(palletId: string, destinationLocationId: string): Observable<string> {
    const command = { palletId, destinationLocationId };
    return this.http.post<string>(`${this.inventoryUrl}/manual-putaway`, command);
  }
  executeTransfer(command: TransferPalletCommand): Observable<string> {
    return this.http.post<string>(`${this.inventoryUrl}/transfer-pallet`, command);
  }

  transferItemsToNewPallet(command: TransferItemsToNewPalletCommand): Observable<string> {
    return this.http.post<string>(`${this.inventoryUrl}/transfer-items`, command);
  }

  recordBlastFreeze(palletId: string): Observable<void> {
    const command: RecordVasCommand = { palletId, serviceType: 'Blasting' };
    return this.http.post<void>(`${this.inventoryUrl}/record-vas`, command);
  }

  recordRepack(
    sourceInventoryId: string,
    targetMaterialId: string,
    quantityToProcess: number,
    durationHours: number
  ): Observable<void> {
    const command: RecordVasCommand = {
      sourceInventoryId,
      targetMaterialId,
      quantityToProcess,
      durationHours,
      serviceType: 'Repack',
    };
    return this.http.post<void>(`${this.inventoryUrl}/record-vas`, command);
  }

  recordLabeling(command: RecordLabelingCommand): Observable<void> {
    const vasCommand: RecordVasCommand = {
      targetId: command.targetId,
      targetType: command.targetType,
      labelType: command.labelType,
      quantityLabeled: command.quantityLabeled,
      serviceType: 'Labeling',
    };
    return this.http.post<void>(`${this.inventoryUrl}/record-vas`, vasCommand);
  }

  startQuarantine(command: StartQuarantineCommand): Observable<void> {
    return this.http.post<void>(`${this.inventoryUrl}/start-quarantine`, command);
  }

  completeFumigation(command: CompleteFumigationCommand): Observable<void> {
    const vasCommand: RecordVasCommand = {
      inventoryId: command.inventoryId,
      durationHours: command.durationHours,
      serviceType: 'Fumigation',
    };

    return this.http.post<void>(`${this.inventoryUrl}/record-vas`, vasCommand);
  }

  recordCycleCount(command: RecordCycleCountCommand): Observable<void> {
    const vasCommand: RecordVasCommand = {
      countedItems: command.countedItems,
      durationHours: command.durationHours,
      serviceType: 'CycleCount',
    };
    return this.http.post<void>(`${this.inventoryUrl}/record-vas`, vasCommand);
  }

  createKit(command: CreateKitCommand): Observable<string> {
    return this.http.post<string>(`${this.inventoryUrl}/create-kit`, command);
  }

  // VAS Amendment Methods
  getVasTransactions(
    accountId: string,
    startDate: Date,
    endDate: Date,
    includeVoided: boolean = false
  ): Observable<VasTransactionDto[]> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString())
      .set('includeVoided', includeVoided.toString());
    
    return this.http.get<VasTransactionDto[]>(
      `${this.apiUrl}/vas/accounts/${accountId}/transactions`,
      { params }
    );
  }

  getVasTransactionDetails(transactionId: string): Observable<VasTransactionDetailDto> {
    return this.http.get<VasTransactionDetailDto>(
      `${this.apiUrl}/vas/transactions/${transactionId}`
    );
  }

  amendVasTransactionLine(
    transactionId: string,
    lineId: string,
    newQuantity: number | null,
    newWeight: number | null,
    reason: string
  ): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/vas/transactions/${transactionId}/lines/${lineId}/amend`,
      {
        newQuantity,
        newWeight,
        amendmentReason: reason
      }
    );
  }

  voidVasTransaction(transactionId: string, reason: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/vas/transactions/${transactionId}/void`,
      {
        voidReason: reason
      }
    );
  }

  getScaleWeight(): Observable<WeightDto> {
    return this.http.get<WeightDto>(`${this.apiUrl}/Scale/weight`);
  }
}
