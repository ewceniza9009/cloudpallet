import {
  AfterViewInit,
  Component,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatExpansionModule } from '@angular/material/expansion';
import { HttpClient } from '@angular/common/http';
import { merge, startWith, switchMap, map, Subject, debounceTime } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ReportsApiService, ActivityLogDto } from '../reports-api.service';
import { Observable } from 'rxjs';
import { UserDto } from '../../admin/admin-api.service';

interface LookupDto {
  id: string;
  name: string;
}
interface UserLookupDto {
  id: string;
  name: string;
}
interface TruckLookupDto {
  id: string;
  name: string;
}

interface ApiTruckDto {
  id: string;
  licensePlate: string;
}

@Component({
  selector: 'app-activity-log',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatAutocompleteModule,
    MatExpansionModule,
  ],
  templateUrl: './activity-log.component.html',
  styleUrls: ['./activity-log.component.scss'],
})
export class ActivityLogComponent implements OnInit, AfterViewInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private reportsApi = inject(ReportsApiService);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  accountControl = new FormControl<string | LookupDto | null>(null);
  userControl = new FormControl<string | UserLookupDto | null>(null);
  truckControl = new FormControl<string | TruckLookupDto | null>(null);

  filterForm = this.fb.group({
    startDate: [null as Date | null],
    endDate: [null as Date | null],
    accountId: [null as string | null],
    userId: [null as string | null],
    truckId: [null as string | null],
  });

  private filterChange = new Subject<void>();

  accounts = signal<LookupDto[]>([]);
  users = signal<UserLookupDto[]>([]);
  trucks = signal<TruckLookupDto[]>([]);

  filteredAccounts$!: Observable<LookupDto[]>;
  filteredUsers$!: Observable<UserLookupDto[]>;
  filteredTrucks$!: Observable<TruckLookupDto[]>;

  isLoading = signal(true);
  resultsLength = signal(0);
  data = signal<ActivityLogDto[]>([]);

  ngOnInit(): void {
    this.loadLookups();
    this.setupFilters();
  }

  ngAfterViewInit(): void {
    merge(this.paginator.page, this.filterChange.pipe(debounceTime(400)))
      .pipe(
        startWith({}),
        switchMap(() => {
          this.isLoading.set(true);
          const filters = this.filterForm.value;
          return this.reportsApi.getActivityLog({
            page: this.paginator.pageIndex + 1,
            pageSize: this.paginator.pageSize,
            startDate: filters.startDate
              ? filters.startDate.toISOString()
              : undefined,
            endDate: filters.endDate
              ? filters.endDate.toISOString()
              : undefined,
            accountId: filters.accountId ?? undefined,
            userId: filters.userId ?? undefined,
            truckId: filters.truckId ?? undefined,
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

  loadLookups(): void {
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe((res) => this.accounts.set(res));

    this.http
      .get<UserDto[]>(`${environment.apiUrl}/Admin/users`)
      .subscribe((res) =>
        this.users.set(
          res.map((u) => ({ id: u.id, name: `${u.firstName} ${u.lastName}` }))
        )
      );

    this.http
      .get<ApiTruckDto[]>(`${environment.apiUrl}/Lookups/trucks`)
      .subscribe((res) =>
        this.trucks.set(res.map((t) => ({ id: t.id, name: t.licensePlate })))
      );
  }

  setupFilters(): void {
    this.filteredAccounts$ = this.accountControl.valueChanges.pipe(
      startWith(''),
      map((value) => this._filter(value, this.accounts()))
    );
    this.filteredUsers$ = this.userControl.valueChanges.pipe(
      startWith(''),
      map((value) => this._filter(value, this.users()))
    );
    this.filteredTrucks$ = this.truckControl.valueChanges.pipe(
      startWith(''),
      map((value) => this._filter(value, this.trucks()))
    );
  }

  getDisplayName(item: { name: string } | string | null): string {
    if (!item || typeof item === 'string') return item || '';
    return item.name;
  }

  private _filter(
    value: string | { name: string } | null,
    options: { id: string; name: string }[]
  ): { id: string; name: string }[] {
    const filterValue = (
      typeof value === 'string'
        ? value
        : (value as { name: string })?.name || ''
    ).toLowerCase();
    return options.filter((option) =>
      option.name.toLowerCase().includes(filterValue)
    );
  }

  onOptionSelected(
    event: MatAutocompleteSelectedEvent,
    formControl: FormControl
  ): void {
    formControl.setValue(event.option.value.id);
  }

  applyFilters(): void {
    this.filterForm.controls.accountId.setValue(
      this.getControlId(this.accountControl)
    );
    this.filterForm.controls.userId.setValue(
      this.getControlId(this.userControl)
    );
    this.filterForm.controls.truckId.setValue(
      this.getControlId(this.truckControl)
    );
    this.paginator.pageIndex = 0;
    this.filterChange.next();
  }

  private getControlId(control: FormControl<any>): string | null {
    const value = control.value;
    return value && typeof value !== 'string' ? value.id : null;
  }

  resetFilters(): void {
    this.filterForm.reset();
    this.accountControl.reset();
    this.userControl.reset();
    this.truckControl.reset();
    this.applyFilters();
  }

  getIconForAction(action: string): string {
    switch (action.toLowerCase()) {
      case 'put away':
        return 'move_to_inbox';
      case 'repack':
        return 'transform';
      case 'picking':
        return 'inventory';
      case 'receiving':
        return 'inventory_2';
      default:
        return 'history';
    }
  }
}
