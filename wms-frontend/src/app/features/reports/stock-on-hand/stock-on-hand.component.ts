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
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatExpansionModule } from '@angular/material/expansion';
import { HttpClient } from '@angular/common/http';
import {
  merge,
  startWith,
  switchMap,
  map,
  Subject,
  debounceTime,
  catchError,
  of,
} from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ReportsApiService,
  StockOnHandDto,
  StockOnHandFilter,
} from '../reports-api.service';

interface LookupDto {
  id: string;
  name: string;
}
interface MaterialLookupDto {
  id: string;
  name: string;
  sku: string;
}

@Component({
  selector: 'app-stock-on-hand',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatProgressSpinnerModule,
    MatAutocompleteModule,
    MatExpansionModule,
  ],
  templateUrl: './stock-on-hand.component.html',
  styleUrls: ['./stock-on-hand.component.scss'],
})
export class StockOnHandComponent implements OnInit, AfterViewInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private reportsApi = inject(ReportsApiService);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  accountControl = new FormControl<string | LookupDto | null>(null);
  materialControl = new FormControl<string | MaterialLookupDto | null>(null);
  supplierControl = new FormControl<string | LookupDto | null>(null);

  filterForm = this.fb.group({
    accountId: [null as string | null],
    materialId: [null as string | null],
    supplierId: [null as string | null],
    batchNumber: [''],
    barcode: [''],
  });

  private filterChange = new Subject<void>();

  accounts = signal<LookupDto[]>([]);
  materials = signal<MaterialLookupDto[]>([]);
  suppliers = signal<LookupDto[]>([]);

  filteredAccounts$ = this.accountControl.valueChanges.pipe(
    startWith(''),
    map((value) => this._filter(value, this.accounts()))
  );
  filteredMaterials$ = this.materialControl.valueChanges.pipe(
    startWith(''),
    map((value) => this._filter(value, this.materials(), true))
  );
  filteredSuppliers$ = this.supplierControl.valueChanges.pipe(
    startWith(''),
    map((value) => this._filter(value, this.suppliers()))
  );

  isLoading = signal(true);
  resultsLength = signal(0);
  dataSource = new MatTableDataSource<StockOnHandDto>([]);

  displayedColumns = [
    'materialName',
    'accountName',
    'supplierName',
    'location',
    'barcodes',
    'batchNumber',
    'expiryDate',
    'quantity',
    'weight',
  ];

  ngOnInit(): void {
    this.loadLookups();
  }

  ngAfterViewInit(): void {
    this.sort.sortChange.subscribe(() => (this.paginator.pageIndex = 0));

    merge(
      this.sort.sortChange,
      this.paginator.page,
      this.filterChange.pipe(debounceTime(300))
    )
      .pipe(
        startWith({}),
        switchMap(() => {
          this.isLoading.set(true);
          this.updateFilterFormIDs();

          const filters: StockOnHandFilter = {
            page: this.paginator.pageIndex + 1,
            pageSize: this.paginator.pageSize,
            sortBy: this.sort.active,
            sortDirection: this.sort.direction || undefined,
            ...(this.filterForm.value as any),
          };

          return this.reportsApi.getStockOnHandReport(filters).pipe(
            catchError(() => {
              console.error('Error fetching stock on hand data');
              return of(null);
            })
          );
        }),
        map((data) => {
          this.isLoading.set(false);
          if (data) {
            this.resultsLength.set(data.totalCount);
            return data.items;
          }
          this.resultsLength.set(0);
          return [];
        })
      )
      .subscribe((data) => (this.dataSource.data = data));
  }

  getDisplayName(item: { name: string } | string | null): string {
    if (!item || typeof item === 'string') return item || '';
    return item.name;
  }

  private _filter(
    value: string | { name: string } | null,
    options: any[],
    isMaterial: boolean = false
  ): any[] {
    const filterValue = (
      typeof value === 'string'
        ? value
        : (value as { name: string })?.name || ''
    ).toLowerCase();
    return options.filter(
      (option) =>
        option.name.toLowerCase().includes(filterValue) ||
        (isMaterial &&
          option.sku &&
          option.sku.toLowerCase().includes(filterValue))
    );
  }

  onOptionSelected(
    event: MatAutocompleteSelectedEvent,
    formControl: FormControl
  ): void {
    formControl.setValue((event.option.value as LookupDto).id);
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
    this.applyFilters();
  }

  loadLookups(): void {
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe((res) => this.accounts.set(res));
    this.http
      .get<MaterialLookupDto[]>(`${environment.apiUrl}/Lookups/materials`)
      .subscribe((res) => this.materials.set(res));
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/suppliers`)
      .subscribe((res) => this.suppliers.set(res));
  }
}
