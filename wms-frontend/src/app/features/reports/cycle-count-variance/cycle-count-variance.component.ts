import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatNativeDateModule } from '@angular/material/core';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { HttpClient } from '@angular/common/http';
import { merge, startWith, switchMap, map, Subject, debounceTime } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ReportsApiService, CycleCountVarianceDto } from '../reports-api.service';

interface LookupDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-cycle-count-variance',
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
    MatDatepickerModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatProgressSpinnerModule,
    MatNativeDateModule,
    MatAutocompleteModule
  ],
  templateUrl: './cycle-count-variance.component.html',
  styleUrls: ['./cycle-count-variance.component.scss']
})
export class CycleCountVarianceComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private reportsApi = inject(ReportsApiService);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  accountControl = new FormControl<string | LookupDto | null>(null);
  materialControl = new FormControl<string | LookupDto | null>(null);

  filterForm = this.fb.group({
    startDate: [null as Date | null],
    endDate: [null as Date | null],
    accountId: [null as string | null],
    materialId: [null as string | null]
  });

  private filterChange = new Subject<void>();

  accounts = signal<LookupDto[]>([]);
  materials = signal<LookupDto[]>([]);

  filteredAccounts$ = this.accountControl.valueChanges.pipe(
    startWith(''),
    map((value) => this._filter(value, this.accounts()))
  );
  filteredMaterials$ = this.materialControl.valueChanges.pipe(
    startWith(''),
    map((value) => this._filter(value, this.materials()))
  );

  isLoading = signal(true);
  resultsLength = signal(0);
  data = signal<CycleCountVarianceDto[]>([]);

  displayedColumns = [
    'timestamp',
    'materialName',
    'locationName',
    'palletBarcode',
    'varianceQuantity',
    'userName'
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
          this.updateFilterFormIDs();
          const filters = this.filterForm.value;

          return this.reportsApi.getCycleCountVariances({
            page: this.paginator.pageIndex + 1,
            pageSize: this.paginator.pageSize,
            sortBy: this.sort.active,
            sortDirection: this.sort.direction || undefined,
            startDate: filters.startDate ? filters.startDate.toISOString() : undefined,
            endDate: filters.endDate ? filters.endDate.toISOString() : undefined,
            accountId: filters.accountId ?? undefined,
            materialId: filters.materialId ?? undefined
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

  private _filter(value: string | LookupDto | null, options: LookupDto[]): LookupDto[] {
    const filterValue = (typeof value === 'string' ? value : (value as LookupDto)?.name || '').toLowerCase();
    return options.filter((option) => option.name.toLowerCase().includes(filterValue));
  }

  onOptionSelected(event: MatAutocompleteSelectedEvent, formControl: FormControl): void {
    const selectedItem = event.option.value;
    formControl.setValue(selectedItem.id);
  }

  updateFilterFormIDs(): void {
    this.filterForm.controls.accountId.setValue(this.getControlId(this.accountControl));
    this.filterForm.controls.materialId.setValue(this.getControlId(this.materialControl));
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
    this.filterChange.next();
  }

  loadLookups(): void {
    this.http.get<LookupDto[]>(`${environment.apiUrl}/Lookups/accounts`).subscribe((res) => {
      this.accounts.set(res);
      this.accountControl.updateValueAndValidity({ emitEvent: true });
    });
    this.http.get<LookupDto[]>(`${environment.apiUrl}/Lookups/materials`).subscribe((res) => {
      this.materials.set(res);
      this.materialControl.updateValueAndValidity({ emitEvent: true });
    });
  }
}
