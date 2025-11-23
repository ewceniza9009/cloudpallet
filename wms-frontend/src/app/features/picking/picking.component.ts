import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCheckboxModule } from '@angular/material/checkbox';
import {
  filter,
  startWith,
  map,
  debounceTime,
  distinctUntilChanged,
} from 'rxjs';
import {
  ConfirmPickByScanRequest,
  ConfirmPickRequest,
  PickItem,
  PickingApiService,
  PickListGroupDto,
  CreatePickListRequest,
} from './picking-api.service';
import {
  ScanConfirmationDialogComponent,
  ScanDialogData,
} from './scan-confirmation-dialog/scan-confirmation-dialog.component';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { WarehouseStateService } from '../../core/services/warehouse-state.service';
import { Observable } from 'rxjs';
import { ScrollingModule } from '@angular/cdk/scrolling';

interface MaterialDto {
  id: string;
  name: string;
  sku: string;
}
interface AccountDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-picking',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatListModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatTooltipModule,
    MatCheckboxModule,
    ScrollingModule,
  ],
  templateUrl: './picking.component.html',
  styleUrls: ['./picking.component.scss'],
})
export class PickingComponent implements OnInit {
  private pickingApi = inject(PickingApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private warehouseState = inject(WarehouseStateService);

  pickListGroups = signal<PickListGroupDto[]>([]);
  isLoading = signal(true);
  isCreating = signal(false);
  materials = signal<MaterialDto[]>([]);
  accounts = signal<AccountDto[]>([]);
  filteredAccounts = signal<AccountDto[]>([]);

  isFormCollapsed = signal(false);

  orderForm: FormGroup;

  constructor() {
    this.orderForm = this.fb.group({
      accountId: ['', Validators.required],
      accountSearch: [''],
      isExpedited: [false],
      orderItems: this.fb.array([], Validators.required),
    });

    this.orderItems.disable();
  }

  ngOnInit(): void {
    this.loadPickList();
    this.loadAccounts();
    this.setupAccountFilter();
  }

  get orderItems(): FormArray {
    return this.orderForm.get('orderItems') as FormArray;
  }

  createOrderItem(): FormGroup {
    const orderItemGroup = this.fb.group({
      materialId: ['', Validators.required],
      materialSearch: [''],
      quantity: [1, [Validators.required, Validators.min(1)]],

      filteredMaterials: new FormControl<MaterialDto[]>([]),
    });

    orderItemGroup
      .get('materialSearch')
      ?.valueChanges.pipe(
        startWith(''),
        debounceTime(300),
        distinctUntilChanged(),

        map((value) => this._filterMaterials(value || ''))
      )
      .subscribe((filtered) => {
        orderItemGroup.get('filteredMaterials')?.setValue(filtered);
      });

    return orderItemGroup;
  }

  addOrderItem(): void {
    this.orderItems.push(this.createOrderItem());
  }

  removeOrderItem(index: number): void {
    if (this.orderItems.length > 1) {
      this.orderItems.removeAt(index);
    } else {
      this.orderItems.at(0).reset({ quantity: 1, filteredMaterials: [] });
    }
  }

  private setupAccountFilter(): void {
    this.orderForm
      .get('accountSearch')
      ?.valueChanges.pipe(
        startWith(''),

        map((value) => (typeof value === 'string' ? value : value?.name)),
        map((name) =>
          name ? this._filterAccounts(name) : this.accounts().slice()
        )
      )
      .subscribe((filtered) => this.filteredAccounts.set(filtered));
  }

  private _filterAccounts(value: string): AccountDto[] {
    const filterValue = value.toLowerCase();
    return this.accounts().filter((acc) =>
      acc.name.toLowerCase().includes(filterValue)
    );
  }

  private _filterMaterials(value: string | MaterialDto): MaterialDto[] {
    const filterValue = (
      typeof value === 'string' ? value : value?.name || ''
    ).toLowerCase();
    return this.materials().filter(
      (mat) =>
        mat.name.toLowerCase().includes(filterValue) ||
        (mat.sku && mat.sku.toLowerCase().includes(filterValue))
    );
  }

  displayAccountName(account: AccountDto): string {
    return account?.name || '';
  }

  displayMaterialName(material: MaterialDto): string {
    return material ? `${material.name} (${material.sku})` : '';
  }

  onMaterialSelected(material: MaterialDto, index: number): void {
    this.orderItems.at(index).get('materialId')?.setValue(material.id);
  }

  onAccountChange(account: AccountDto): void {
    this.materials.set([]);
    this.orderItems.clear();

    if (account?.id) {
      this.orderForm.get('accountId')?.setValue(account.id);
      this.orderItems.enable();
      this.addOrderItem();
      this.loadMaterialsForAccount(account.id);
    } else {
      this.orderForm.get('accountId')?.setValue('');
      this.orderItems.disable();
    }
  }

  loadAccounts(): void {
    this.http
      .get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe((data) => {
        this.accounts.set(data);

        this.orderForm.get('accountSearch')?.updateValueAndValidity();
      });
  }

  loadMaterialsForAccount(accountId: string): void {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) {
      this.snackBar.open('Please select a warehouse first.', 'Close');
      return;
    }

    const params = new HttpParams()
      .set('warehouseId', warehouseId)
      .set('accountId', accountId);
    this.http
      .get<MaterialDto[]>(`${environment.apiUrl}/Lookups/pickable-materials`, {
        params,
      })
      .subscribe((data) => {
        this.materials.set(data);

        this.orderItems.controls.forEach((control) => {
          control.get('materialSearch')?.setValue('');
        });
      });
  }

  createPickList(): void {
    this.orderForm.markAllAsTouched();
    if (this.orderForm.invalid) {
      this.snackBar.open(
        'Please select an account and fill out all order items correctly.',
        'Close',
        { duration: 4000 }
      );
      return;
    }
    this.isCreating.set(true);

    const payload: CreatePickListRequest = {
      isExpedited: this.orderForm.value.isExpedited,
      orderItems: this.orderForm.value.orderItems.map((item: any) => ({
        materialId: item.materialId,
        quantity: item.quantity,
      })),
    };

    payload.orderItems = payload.orderItems.filter(
      (item) => item.materialId && item.quantity > 0
    );

    if (payload.orderItems.length === 0) {
      this.snackBar.open(
        'No valid order items to create a pick list.',
        'Close',
        { duration: 4000 }
      );
      this.isCreating.set(false);
      return;
    }

    this.pickingApi.createPickList(payload).subscribe({
      next: () => {
        this.snackBar.open('New pick list created successfully!', 'OK', {
          duration: 3000,
        });

        this.orderForm.reset({
          accountId: '',
          accountSearch: '',
          isExpedited: false,
        });
        this.orderItems.clear();
        this.orderItems.disable();
        this.materials.set([]);
        this.loadPickList();
        this.isCreating.set(false);
      },
      error: (err) => {
        const message =
          err.error?.title ||
          'Failed to create pick list. Insufficient inventory?';
        this.snackBar.open(message, 'Close', { duration: 7000 });
        this.isCreating.set(false);
      },
    });
  }

  loadPickList(): void {
    this.isLoading.set(true);
    this.pickingApi.getPickList().subscribe({
      next: (data) => {
        this.pickListGroups.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load pick list', err);
        this.snackBar.open('Could not load pick list.', 'Close', {
          duration: 5000,
        });
        this.isLoading.set(false);
      },
    });
  }

  openScanDialog(item: PickItem): void {
    const dialogRef = this.dialog.open<
      ScanConfirmationDialogComponent,
      ScanDialogData
    >(ScanConfirmationDialogComponent, {
      width: '400px',
      data: {
        pickId: item.pickId,
        material: item.material,
        location: item.location,
      },
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result) => !!result))
      .subscribe((result) => {
        const request: ConfirmPickByScanRequest = {
          pickTransactionId: item.pickId,
          scannedLocationCode: result.scannedLocationCode,
          scannedLpn: result.scannedLpn,
          actualWeight: result.actualWeight,
        };

        this.pickingApi.confirmPickByScan(request).subscribe({
          next: () => this.updateItemStatus(item.pickId, 'Confirmed'),
          error: (err) =>
            this.snackBar.open(
              `Scan Confirmation Failed: ${
                err.error?.detail || err.error?.title || 'Check console.'
              }`,
              'Close',
              { duration: 7000 }
            ),
        });
      });
  }

  markAsShort(item: PickItem): void {
    const request: ConfirmPickRequest = {
      pickTransactionId: item.pickId,
      newStatus: 'Short',
    };

    this.pickingApi.confirmPickManually(request).subscribe({
      next: () => this.updateItemStatus(item.pickId, 'Short'),
      error: (err) =>
        this.snackBar.open(
          `Failed to mark as short: ${err.error?.title || 'Unknown error'}`,
          'Close',
          { duration: 5000 }
        ),
    });
  }

  private updateItemStatus(
    pickId: string,
    newStatus: 'Confirmed' | 'Short'
  ): void {
    this.pickListGroups.update((groups) =>
      groups.map((g) => ({
        ...g,
        items: g.items.map((item) =>
          item.pickId === pickId ? { ...item, status: newStatus } : item
        ),
      }))
    );
    this.snackBar.open(`Item marked as ${newStatus}.`, 'OK', {
      duration: 3000,
    });
  }
}
