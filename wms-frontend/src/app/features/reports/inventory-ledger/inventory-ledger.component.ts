import {
  AfterViewInit,
  Component,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatNativeDateModule } from '@angular/material/core';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatExpansionModule } from '@angular/material/expansion';
import { HttpClient } from '@angular/common/http';
import { merge, startWith, switchMap, map, Subject, debounceTime } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ReportsApiService,
  InventoryLedgerGroupDto,
} from '../reports-api.service';
import {
  trigger,
  state,
  style,
  transition,
  animate,
} from '@angular/animations';
import { WarehouseStateService } from '../../../core/services/warehouse-state.service';

interface LookupDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-inventory-ledger',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatProgressSpinnerModule,
    MatNativeDateModule,
    MatAutocompleteModule,
    MatExpansionModule,
  ],
  templateUrl: './inventory-ledger.component.html',
  styleUrls: ['./inventory-ledger.component.scss'],
  animations: [
    trigger('detailExpand', [
      state(
        'collapsed,void',
        style({ height: '0px', minHeight: '0', padding: '0' })
      ),
      state('expanded', style({ height: '*' })),
      transition(
        'expanded <=> collapsed',
        animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')
      ),
    ]),
  ],
})
export class InventoryLedgerComponent implements OnInit, AfterViewInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private reportsApi = inject(ReportsApiService);
  private warehouseState = inject(WarehouseStateService);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  accountControl = new FormControl<string | LookupDto | null>(null);
  materialControl = new FormControl<string | LookupDto | null>(null);
  supplierControl = new FormControl<string | LookupDto | null>(null);

  filterForm = this.fb.group({
    startDate: [null as Date | null],
    endDate: [null as Date | null],
    accountId: [null as string | null],
    materialId: [null as string | null],
    supplierId: [null as string | null],
  });

  private filterChange = new Subject<void>();

  accounts = signal<LookupDto[]>([]);
  materials = signal<LookupDto[]>([]);
  suppliers = signal<LookupDto[]>([]);

  filteredAccounts$ = this.accountControl.valueChanges.pipe(
    startWith(''),
    map((value) => this._filter(value, this.accounts()))
  );
  filteredMaterials$ = this.materialControl.valueChanges.pipe(
    startWith(''),
    map((value) => this._filter(value, this.materials()))
  );
  filteredSuppliers$ = this.supplierControl.valueChanges.pipe(
    startWith(''),
    map((value) => this._filter(value, this.suppliers()))
  );

  isLoading = signal(true);
  resultsLength = signal(0);
  data = signal<InventoryLedgerGroupDto[]>([]);
  expandedElement: InventoryLedgerGroupDto | null = null;
  loadingDetails = signal<Set<string>>(new Set());

  displayedColumns = [
    'materialName',
    'totalQtyIn',
    'totalQtyOut',
    'netQtyChange',
    'totalWgtIn',
    'totalWgtOut',
    'netWgtChange',
    'expand',
  ];

  ngOnInit(): void {
    this.loadLookups();
  }

  ngAfterViewInit(): void {
    this.sort.sortChange.subscribe(() => (this.paginator.pageIndex = 0));

    merge(
      this.sort.sortChange,
      this.paginator.page,
      this.filterChange.pipe(debounceTime(400))
    )
      .pipe(
        startWith({}),
        switchMap(() => {
          this.isLoading.set(true);
          this.expandedElement = null;
          this.updateFilterFormIDs();
          const filters = this.filterForm.value;
          const warehouseId = this.warehouseState.selectedWarehouseId();

          return this.reportsApi.getInventoryLedger({
            page: this.paginator.pageIndex + 1,
            pageSize: this.paginator.pageSize,
            sortBy: this.sort.active,
            sortDirection: this.sort.direction || undefined,
            startDate: filters.startDate
              ? filters.startDate.toISOString()
              : undefined,
            endDate: filters.endDate
              ? filters.endDate.toISOString()
              : undefined,
            accountId: filters.accountId ?? undefined,
            materialId: filters.materialId ?? undefined,
            supplierId: filters.supplierId ?? undefined,
          });
        }),
        map((data) => {
          this.isLoading.set(false);
          this.resultsLength.set(data.totalCount);
          return data.items;
        })
      )
      .subscribe((data) => this.data.set(data));
  }

  getDisplayName(item: LookupDto | string | null): string {
    if (!item || typeof item === 'string') return item || '';
    return item.name;
  }

  private _filter(
    value: string | LookupDto | null,
    options: LookupDto[]
  ): LookupDto[] {
    const filterValue = (
      typeof value === 'string' ? value : (value as LookupDto)?.name || ''
    ).toLowerCase();
    return options.filter((option) =>
      option.name.toLowerCase().includes(filterValue)
    );
  }

  onOptionSelected(
    event: MatAutocompleteSelectedEvent,
    formControl: FormControl
  ): void {
    const selectedItem = event.option.value;
    formControl.setValue(selectedItem.id);
  }

  updateFilterFormIDs(): void {
    this.filterForm.controls.accountId.setValue(
      this.getControlId(this.accountControl)
    );
    this.filterForm.controls.materialId.setValue(
      this.getControlId(this.materialControl)
    );
    this.filterForm.controls.supplierId.setValue(
      this.getControlId(this.supplierControl)
    );
  }

  private getControlId(control: FormControl<any>): string | null {
    const value = control.value;
    return value && typeof value !== 'string' ? value.id : null;
  }

  applyFilters(): void {
    this.paginator.pageIndex = 0;
    this.filterChange.next();
  }

  resetFilters(): void {
    this.filterForm.reset();
    this.accountControl.reset();
    this.materialControl.reset();
    this.supplierControl.reset();
    this.filterChange.next();
  }

  loadLookups(): void {
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe((res) => this.accounts.set(res));
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/materials`)
      .subscribe((res) => this.materials.set(res));
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/suppliers`)
      .subscribe((res) => this.suppliers.set(res));
  }

  getIconForEventType(type: string): string {
    switch (type.toLowerCase()) {
      case 'receiving':
        return 'inventory_2';
      case 'picking':
        return 'inventory';
      case 'repack':
        return 'transform';
      case 'kitting':
        return 'build';
      case 'count':
        return 'checklist_rtl';
      case 'damage':
        return 'broken_image';
      case 'expiry':
        return 'event_busy';
      case 'item transfer':
        return 'compare_arrows';
      default:
        return 'sync_alt';
    }
  }

  getColorClassForEventType(type: string): string {
    switch (type.toLowerCase()) {
      case 'receiving':
        return 'event-inbound';
      case 'picking':
        return 'event-outbound';
      case 'repack':
      case 'kitting':
      case 'count':
      case 'damage':
      case 'expiry':
      case 'item transfer':
        return 'event-internal';
      default:
        return 'event-internal';
    }
  }
  toggleRow(group: InventoryLedgerGroupDto): void {
    if (this.expandedElement === group) {
      this.expandedElement = null;
      return;
    }

    this.expandedElement = group;

    if (group.lines && group.lines.length > 0) {
      return; // Already loaded
    }

    // Check if already loading
    if (this.loadingDetails().has(group.materialId)) {
      return;
    }

    // Mark as loading
    this.loadingDetails.update(set => {
      const newSet = new Set(set);
      newSet.add(group.materialId);
      return newSet;
    });

    const filters = this.filterForm.value;

    this.reportsApi.getInventoryLedgerDetails({
      page: 1,
      pageSize: 10000,
      startDate: filters.startDate ? filters.startDate.toISOString() : undefined,
      endDate: filters.endDate ? filters.endDate.toISOString() : undefined,
      accountId: filters.accountId ?? undefined,
      materialId: group.materialId,
      supplierId: filters.supplierId ?? undefined,
    }).subscribe({
      next: (lines) => {
        group.lines = lines;
        this.loadingDetails.update(set => {
          const newSet = new Set(set);
          newSet.delete(group.materialId);
          return newSet;
        });
      },
      error: () => {
        this.loadingDetails.update(set => {
          const newSet = new Set(set);
          newSet.delete(group.materialId);
          return newSet;
        });
      }
    });
  }
}
