import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ViewChild,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatTableModule } from '@angular/material/table';
import {
  ScrollingModule,
  CdkVirtualScrollViewport,
} from '@angular/cdk/scrolling';
import { HttpClient } from '@angular/common/http';
import {
  forkJoin,
  filter,
  switchMap,
  Subscription,
  tap,
  debounceTime,
  distinctUntilChanged,
  map,
  startWith,
  Observable,
} from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InventoryApiService,
  MaterialDto,
  CreateReceivingSessionCommand,
  AddPalletToReceivingCommand,
  ReceivingVarianceDto,
} from '../inventory/inventory-api.service';
import {
  ProcessLineDialogComponent,
  ProcessLineDialogData,
  ProcessLineDialogResult,
} from './process-line-dialog/process-line-dialog.component';
import { ConfirmationDialogComponent } from '../../shared/confirmation-dialog/confirmation-dialog.component';
import { DockApiService } from '../../features/dock-scheduling/dock-api.service';
import {
  SelectPalletTypeDialogComponent,
  SelectPalletTypeResult,
} from './select-pallet-type-dialog/select-pallet-type-dialog.component';
import { BarcodePrintDialogComponent } from '../../shared/barcode-print-dialog/barcode-print-dialog.component';

interface PalletLineState {
  palletLineId: string;
  materialId: string;
  materialName: string;
  netWeight?: number;
  barcode?: string;
  status: 'Pending' | 'Processed';
  quantity?: number;
  batchNumber?: string;
  dateOfManufacture?: string;
  expiryDate?: string;
}
interface PalletState {
  id: string;
  palletNumber: number;
  palletTypeName: string;
  tareWeight: number;
  lines: PalletLineState[];
  materialToAdd: FormControl<string | null>;
}
interface SupplierDto {
  id: string;
  name: string;
}
interface AppointmentDto {
  id: string;
  licensePlate: string;
  startTime: string;
}
interface AccountDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-receiving-session',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatSnackBarModule,
    MatIconModule,
    MatSelectModule,
    MatExpansionModule,
    MatDialogModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatAutocompleteModule,
    ScrollingModule,
    MatProgressBarModule,
    DatePipe,
    MatDividerModule,
    MatTableModule,
  ],
  templateUrl: './receiving-session.component.html',
  styleUrls: ['./receiving-session.component.scss'],
})
export class ReceivingSessionComponent implements OnInit, OnDestroy {
  @ViewChild(CdkVirtualScrollViewport)
  virtualScrollViewport!: CdkVirtualScrollViewport;

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private inventoryApi = inject(InventoryApiService);
  private dockApi = inject(DockApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  supplierControl = new FormControl<SupplierDto | string | null>(
    null,
    Validators.required
  );
  accountControl = new FormControl<AccountDto | string | null>(
    null,
    Validators.required
  );

  sessionForm: FormGroup;
  private routeSub!: Subscription;

  suppliers = signal<SupplierDto[]>([]);
  appointments = signal<AppointmentDto[]>([]);
  accounts = signal<AccountDto[]>([]);

  filteredSuppliers: Observable<SupplierDto[]>;
  filteredAccounts: Observable<AccountDto[]>;

  receivingSessionId = signal<string | null>(null);
  pallets = signal<PalletState[]>([]);
  isStartingSession = signal(false);
  isAddingPallet = signal(false);
  isNewSession = signal(true);
  variance = signal<ReceivingVarianceDto | null>(null);

  materialSearchCtrl = new FormControl('');
  filteredMaterials = signal<MaterialDto[]>([]);
  isMaterialLoading = signal(false);
  totalMaterials = 0;
  private currentPage = 1;
  private readonly pageSize = 20;
  private materialSearchSub!: Subscription;

  constructor() {
    this.sessionForm = this.fb.group({
      dockAppointmentId: [null as string | null],
      remarks: [''],
    });

    this.loadInitialData();

    this.filteredSuppliers = this.supplierControl.valueChanges.pipe(
      startWith(''),
      map((value) => (typeof value === 'string' ? value : value?.name) || ''),
      map((name) =>
        name ? this._filter(name, this.suppliers()) : this.suppliers().slice()
      )
    );
    this.filteredAccounts = this.accountControl.valueChanges.pipe(
      startWith(''),
      map((value) => (typeof value === 'string' ? value : value?.name) || ''),
      map((name) =>
        name ? this._filter(name, this.accounts()) : this.accounts().slice()
      )
    );

    const navigation = this.router.getCurrentNavigation();
    const appointmentId = navigation?.extras.state?.['appointmentId'];

    if (appointmentId) {
      this.sessionForm.patchValue({ dockAppointmentId: appointmentId });
      this.dockApi.getAppointmentDetails(appointmentId).subscribe((details) => {
        forkJoin({
          suppliers: this.http.get<SupplierDto[]>(
            `${environment.apiUrl}/Lookups/suppliers`
          ),
          accounts: this.http.get<AccountDto[]>(
            `${environment.apiUrl}/Lookups/accounts`
          ),
        }).subscribe(({ suppliers, accounts }) => {
          this.suppliers.set(suppliers);
          this.accounts.set(accounts);

          const prefilledSupplier = suppliers.find(
            (s) => s.id === details.supplierId
          );
          const prefilledAccount = accounts.find(
            (a) => a.id === details.accountId
          );

          if (prefilledSupplier)
            this.supplierControl.setValue(prefilledSupplier);
          if (prefilledAccount) this.accountControl.setValue(prefilledAccount);

          this.supplierControl.disable();
          this.accountControl.disable();
        });
      });
    }
  }

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe((params) => {
      const sessionId = params.get('id');
      if (sessionId && sessionId.toLowerCase() !== 'new') {
        this.isNewSession.set(false);
        this.loadExistingSession(sessionId);
      } else {
        this.isNewSession.set(true);
        if (!this.sessionForm.get('dockAppointmentId')?.value) {
          this.receivingSessionId.set(null);
          this.pallets.set([]);
          this.supplierControl.enable();
          this.accountControl.enable();
          this.sessionForm.reset();
        }
      }
    });
    this.setupMaterialSearch();
  }

  ngOnDestroy(): void {
    if (this.routeSub) this.routeSub.unsubscribe();
    if (this.materialSearchSub) this.materialSearchSub.unsubscribe();
  }

  getLookupName(item: any): string {
    return item && typeof item !== 'string' ? item.name : '';
  }

  displayMaterialName(material: MaterialDto): string {
    return material?.name || '';
  }

  private loadInitialData(): void {
    forkJoin({
      suppliers: this.http.get<SupplierDto[]>(
        `${environment.apiUrl}/Lookups/suppliers`
      ),
      accounts: this.http.get<AccountDto[]>(
        `${environment.apiUrl}/Lookups/accounts`
      ),
      appointments: this.http.get<AppointmentDto[]>(
        `${environment.apiUrl}/Lookups/active-appointments`
      ),
    }).subscribe(({ suppliers, accounts, appointments }) => {
      this.suppliers.set(suppliers);
      this.accounts.set(accounts);
      this.appointments.set(appointments);
    });
  }

  private _filter(
    value: string,
    options: { id: string; name: string }[]
  ): { id: string; name: string }[] {
    const filterValue = value?.toLowerCase() || '';
    return options.filter((option) =>
      option.name.toLowerCase().includes(filterValue)
    );
  }

  private setupMaterialSearch(): void {
    this.materialSearchSub = this.materialSearchCtrl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => {
          this.isMaterialLoading.set(true);
          this.filteredMaterials.set([]);
          this.currentPage = 1;
          this.totalMaterials = 0;
          if (this.virtualScrollViewport) {
            this.virtualScrollViewport.scrollToIndex(0);
          }
        }),
        switchMap((value) =>
          this.inventoryApi.searchMaterials(
            typeof value === 'string' ? value : null,
            this.currentPage,
            this.pageSize
          )
        )
      )
      .subscribe((result) => {
        this.filteredMaterials.set(result.items);
        this.totalMaterials = result.totalCount;
        this.isMaterialLoading.set(false);
      });
    this.materialSearchCtrl.setValue('');
  }

  fetchNextMaterialPage(): void {
    if (
      this.isMaterialLoading() ||
      this.filteredMaterials().length >= this.totalMaterials
    )
      return;
    this.isMaterialLoading.set(true);
    this.currentPage++;
    const searchTerm = this.materialSearchCtrl.value;
    this.inventoryApi
      .searchMaterials(
        typeof searchTerm === 'string' ? searchTerm : null,
        this.currentPage,
        this.pageSize
      )
      .subscribe((result) => {
        this.filteredMaterials.update((current) => [
          ...current,
          ...result.items,
        ]);
        this.isMaterialLoading.set(false);
      });
  }

  onMaterialPanelOpen(): void {
    if (!this.materialSearchCtrl.value) {
      this.materialSearchCtrl.setValue('');
    }
  }

  onMaterialSelected(
    event: MatAutocompleteSelectedEvent,
    pallet: PalletState
  ): void {
    const selectedMaterial: MaterialDto = event.option.value;
    pallet.materialToAdd.setValue(selectedMaterial.id);
  }

  private loadExistingSession(sessionId: string): void {
    this.inventoryApi.getReceivingSessionById(sessionId).subscribe({
      next: (session) => {
        this.receivingSessionId.set(session.receivingId);

        forkJoin({
          suppliers: this.http.get<SupplierDto[]>(
            `${environment.apiUrl}/Lookups/suppliers`
          ),
          accounts: this.http.get<AccountDto[]>(
            `${environment.apiUrl}/Lookups/accounts`
          ),
        }).subscribe(({ suppliers, accounts }) => {
          this.suppliers.set(suppliers);
          this.accounts.set(accounts);

          const prefilledSupplier = suppliers.find(
            (s) => s.id === session.supplierId
          );
          const prefilledAccount = accounts.find(
            (a) => a.id === session.accountId
          );

          this.sessionForm.patchValue({
            dockAppointmentId: session.dockAppointmentId,
          });
          if (prefilledSupplier)
            this.supplierControl.setValue(prefilledSupplier);
          if (prefilledAccount) this.accountControl.setValue(prefilledAccount);

          this.supplierControl.disable();
          this.accountControl.disable();
        });

        const palletStates: PalletState[] = session.pallets.map(
          (pDto: any) => ({
            id: pDto.id,
            palletNumber: parseInt(pDto.palletNumber, 10),
            palletTypeName: pDto.palletTypeName,
            tareWeight: pDto.tareWeight,
            lines: this.mapLinesToState(pDto.lines),
            materialToAdd: new FormControl(''),
          })
        );
        this.pallets.set(palletStates);
        this.loadVariance(sessionId);
      },
      error: () =>
        this.snackBar.open('Failed to load existing session details.', 'Close'),
    });
  }

  private mapLinesToState(lines: any[]): PalletLineState[] {
    return lines.map((l) => ({
      palletLineId: l.palletLineId,
      materialId: l.materialId,
      materialName: l.materialName,
      netWeight: l.netWeight,
      barcode: l.barcode,
      status: l.status,
      quantity: l.quantity,
      batchNumber: l.batchNumber,
      dateOfManufacture: l.dateOfManufacture,
      expiryDate: l.expiryDate,
    }));
  }

  getSelectedSupplier(): SupplierDto | undefined {
    const supplierValue = this.supplierControl.value;
    return typeof supplierValue !== 'string' && supplierValue
      ? supplierValue
      : undefined;
  }

  getProcessedLinesCount(pallet: PalletState): number {
    return pallet.lines.filter(
      (line: PalletLineState) => line.status === 'Processed'
    ).length;
  }

  isPalletBarcodeGenerated(pallet: PalletState): boolean {
    return pallet.lines.some((l) => l.status === 'Processed');
  }
  onPrintPalletBarcode(pallet: PalletState): void {
    this.dialog.open(BarcodePrintDialogComponent, {
      data: {
        title: `SSCC Label (Pallet #${pallet.palletNumber})`,
        barcodeText: pallet.id,
        type: 'Pallet',
      },
    });
  }

  addPallet(): void {
    if (!this.receivingSessionId()) return;

    const dialogRef = this.dialog.open<
      SelectPalletTypeDialogComponent,
      undefined,
      SelectPalletTypeResult
    >(SelectPalletTypeDialogComponent, {
      width: '400px',
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result): result is SelectPalletTypeResult => !!result))
      .subscribe((result: SelectPalletTypeResult) => {
        this.isAddingPallet.set(true);
        const command: AddPalletToReceivingCommand = {
          receivingId: this.receivingSessionId()!,
          palletTypeId: result.palletTypeId,
          isCrossDock: result.isCrossDock,
        };

        this.inventoryApi.addPalletToReceiving(command).subscribe({
          next: () => {
            this.loadExistingSession(this.receivingSessionId()!);
            this.isAddingPallet.set(false);
            this.snackBar.open(
              `Pallet added successfully ${
                result.isCrossDock ? '(Marked for Cross-Dock)' : ''
              }.`,
              'OK',
              { duration: 3000 }
            );
          },
          error: (err) => {
            this.snackBar.open(
              `Failed to add pallet: ${err.error?.title || 'Unknown error'}`,
              'Close',
              { duration: 5000 }
            );
            this.isAddingPallet.set(false);
          },
        });
      });
  }

  addLineToPallet(pallet: PalletState): void {
    const materialIdToAdd = pallet.materialToAdd.value;
    if (!materialIdToAdd) {
      this.snackBar.open('Please select a material to add.', 'Close');
      return;
    }
    if (
      pallet.lines.some(
        (line) =>
          line.materialId === materialIdToAdd && line.status === 'Pending'
      )
    ) {
      this.snackBar.open(
        'This material is already pending processing on the pallet.',
        'Close'
      );
      return;
    }
    this.inventoryApi
      .addLineToPallet(this.receivingSessionId()!, pallet.id, materialIdToAdd)
      .subscribe({
        next: () => {
          this.loadExistingSession(this.receivingSessionId()!);
          pallet.materialToAdd.reset();
          this.materialSearchCtrl.reset();
        },
        error: () =>
          this.snackBar.open('Failed to add material to the pallet.', 'Close'),
      });
  }

  startSession(): void {
    if (this.supplierControl.invalid || this.accountControl.invalid) return;

    const supplierId =
      typeof this.supplierControl.value !== 'string'
        ? (this.supplierControl.value as SupplierDto)?.id
        : null;
    const accountId =
      typeof this.accountControl.value !== 'string'
        ? (this.accountControl.value as AccountDto)?.id
        : null;

    if (!supplierId || !accountId) {
      this.snackBar.open(
        'Please select valid Supplier and Account from the list.',
        'Close'
      );
      return;
    }
    this.isStartingSession.set(true);
    const command: CreateReceivingSessionCommand = {
      supplierId: supplierId,
      dockAppointmentId: this.sessionForm.value.dockAppointmentId,
      accountId: accountId,
      remarks: this.sessionForm.value.remarks,
    };
    this.inventoryApi.createReceivingSession(command).subscribe({
      next: (sessionId) => {
        this.receivingSessionId.set(sessionId);
        this.supplierControl.disable();
        this.accountControl.disable();
        this.isNewSession.set(false);
        this.router.navigate(['/receiving', sessionId], { replaceUrl: true });
        this.isStartingSession.set(false);
      },
      error: (err) => {
        this.snackBar.open(
          `Failed to start session: ${err.error?.title || 'Unknown error'}`,
          'Close'
        );
        this.isStartingSession.set(false);
      },
    });
  }

  deleteLineFromPallet(
    pallet: PalletState,
    lineToDelete: PalletLineState
  ): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Pallet Line',
        message: `Are you sure you want to delete the line for "${lineToDelete.materialName}"? This action cannot be undone.`,
      },
    });
    dialogRef
      .afterClosed()
      .pipe(
        filter((result) => result === true),
        switchMap(() =>
          this.inventoryApi.deletePalletLine(
            this.receivingSessionId()!,
            pallet.id,
            lineToDelete.palletLineId
          )
        )
      )
      .subscribe({
        next: () => {
          this.pallets.update((currentPallets) =>
            currentPallets.map((p) =>
              p.id === pallet.id
                ? {
                    ...p,
                    lines: p.lines.filter(
                      (l) => l.palletLineId !== lineToDelete.palletLineId
                    ),
                  }
                : p
            )
          );
          this.snackBar.open('Pallet line deleted successfully.', 'OK', {
            duration: 3000,
          });
        },
        error: () =>
          this.snackBar.open('Failed to delete pallet line.', 'Close'),
      });
  }

  deletePallet(palletToDelete: PalletState): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Pallet',
        message: `Are you sure you want to delete Pallet #${palletToDelete.palletNumber} and all its lines?`,
      },
    });
    dialogRef
      .afterClosed()
      .pipe(
        filter((result) => result === true),
        switchMap(() =>
          this.inventoryApi.deletePallet(
            this.receivingSessionId()!,
            palletToDelete.id
          )
        )
      )
      .subscribe({
        next: () => {
          this.pallets.update((currentPallets) =>
            currentPallets.filter((p) => p.id !== palletToDelete.id)
          );
          this.snackBar.open('Pallet deleted successfully.', 'OK', {
            duration: 3000,
          });
        },
        error: () => this.snackBar.open('Failed to delete pallet.', 'Close'),
      });
  }

  openProcessLineDialog(
    pallet: PalletState,
    line: PalletLineState,
    isEdit: boolean = false
  ): void {
    const dialogRef = this.dialog.open<
      ProcessLineDialogComponent,
      ProcessLineDialogData,
      ProcessLineDialogResult
    >(ProcessLineDialogComponent, {
      width: '800px',
      maxWidth: '95vw',
      maxHeight: '95vh',
      disableClose: true,
      data: {
        receivingId: this.receivingSessionId()!,
        palletId: pallet.id,
        palletLineId: line.palletLineId,
        materialId: line.materialId,
        materialName: line.materialName,
        palletTareWeight: pallet.tareWeight,
        existingData: isEdit
          ? {
              quantity: line.quantity,
              batchNumber: line.batchNumber,
              dateOfManufacture: line.dateOfManufacture,
              expiryDate: line.expiryDate,
            }
          : undefined,
      },
    });
    dialogRef
      .afterClosed()
      .pipe(filter((result): result is ProcessLineDialogResult => !!result))
      .subscribe((result) => {
        this.pallets.update((pallets) =>
          pallets.map((p) =>
            p.id === pallet.id
              ? {
                  ...p,
                  lines: p.lines.map((l) =>
                    l.palletLineId === line.palletLineId
                      ? {
                          ...l,
                          status: 'Processed',
                          barcode: result!.barcode,
                          netWeight: result!.netWeight,
                          quantity: result!.quantity,
                          batchNumber: result!.batchNumber,
                          dateOfManufacture: result!.dateOfManufacture,
                          expiryDate: result!.expiryDate ?? undefined,
                        }
                      : l
                  ),
                }
              : p
          )
        );
        this.snackBar.open(
          `Line for ${line.materialName} processed successfully!`,
          'OK',
          { duration: 3000 }
        );

        this.dialog.open(BarcodePrintDialogComponent, {
          data: {
            title: `Item LPN Label: ${line.materialName}`,
            barcodeText: result!.barcode,
            type: 'Item',
            quantity: result!.quantity,
          },
        });
        this.loadExistingSession(this.receivingSessionId()!);
      });
  }

  finishSession(): void {
    const sessionId = this.receivingSessionId();
    if (!sessionId) {
      this.snackBar.open(
        'Cannot complete session: Invalid Session ID.',
        'Close'
      );
      return;
    }
    this.inventoryApi.completeReceivingSession(sessionId).subscribe({
      next: () => {
        this.snackBar.open('Receiving session completed successfully!', 'OK', {
          duration: 5000,
        });
        this.router.navigate(['/receiving']);
      },
      error: (err) => {
        this.snackBar.open(
          `Error: ${err.error?.title || 'Failed to complete session.'}`,
          'Close'
        );
      },
    });
  }

  back(): void {
    this.router.navigate(['/receiving']);
  }

  private loadVariance(receivingId: string): void {
    this.inventoryApi.getReceivingVariance(receivingId).subscribe({
      next: (data) => this.variance.set(data),
      error: () => this.variance.set(null),
    });
  }
}
