import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { AdminApiService, Rate, RateDto } from '../../admin-api.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { map, startWith, debounceTime, distinctUntilChanged, merge, tap } from 'rxjs';

interface AccountDto { id: string; name: string; }

@Component({
  selector: 'app-rate-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    CurrencyPipe,
    DatePipe,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatPaginatorModule,
    MatSortModule
  ],
  templateUrl: './rate-list.component.html',
  styleUrls: ['./rate-list.component.scss'],
})
export class RateListComponent implements OnInit {
  private adminApi = inject(AdminApiService);
  private http = inject(HttpClient);
  private router = inject(Router);

  private _paginator: MatPaginator | undefined;
  private _sort: MatSort | undefined;

  @ViewChild(MatPaginator) set paginator(value: MatPaginator) {
    this._paginator = value;
    if (this._paginator) {
      this._paginator.page.pipe(
        tap(() => this.loadData())
      ).subscribe();
    }
  }
  get paginator(): MatPaginator | undefined {
    return this._paginator;
  }

  @ViewChild(MatSort) set sort(value: MatSort) {
    this._sort = value;
    if (this._sort) {
      this._sort.sortChange.pipe(
        tap(() => {
          if (this.paginator) this.paginator.pageIndex = 0;
          this.loadData();
        })
      ).subscribe();
    }
  }
  get sort(): MatSort | undefined {
    return this._sort;
  }

  rates = signal<RateDto[]>([]);
  totalCount = signal(0);
  isLoading = signal(true);
  searchControl = new FormControl('');
  accountMap = new Map<string, string>();

  displayedColumns: string[] = [
    'account',
    'tier',
    'uom',
    'value',
    'dates',
    'status',
    'actions'
  ];

  ngOnInit(): void {
    this.loadAccounts();
    this.loadData();

    // Add debounce for better search performance
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      if (this.paginator) this.paginator.pageIndex = 0;
      this.loadData();
    });
  }

  loadAccounts(): void {
    this.http.get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`).subscribe(accounts => {
        accounts.forEach(acc => this.accountMap.set(acc.id, acc.name));
        this.accountMap.set('00000000-0000-0000-0000-000000000000', 'Default');
    });
  }

  loadData(): void {
    // Note: We don't set isLoading(true) here to avoid flickering/re-rendering the table
    // which would destroy the paginator/sort view children.
    // Only set it on initial load or if you want to hide the table.
    if (this.rates().length === 0) {
        this.isLoading.set(true);
    }
    
    const page = this.paginator ? this.paginator.pageIndex + 1 : 1;
    const pageSize = this.paginator ? this.paginator.pageSize : 10;
    const sortBy = this.sort ? this.sort.active : 'ServiceType';
    const sortDirection = this.sort ? this.sort.direction : 'asc';
    const searchTerm = this.searchControl.value || '';

    this.adminApi.getRates({
      page,
      pageSize,
      sortBy,
      sortDirection: sortDirection === '' ? undefined : sortDirection as 'asc' | 'desc',
      searchTerm
    }).subscribe({
      next: (result) => {
        this.rates.set(result.items);
        this.totalCount.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load rates:', error);
        this.isLoading.set(false);
      }
    });
  }

  getAccountName(accountId: string | null): string {
    if (!accountId || accountId === '00000000-0000-0000-0000-000000000000') return 'Default Rate';
    return this.accountMap.get(accountId) || 'Unknown Account';
  }

  getTierClass(tier: string | null): string {
    if (!tier) return 'default';
    return tier.toLowerCase().replace(/\s+/g, '');
  }

  createNew(): void {
    this.router.navigate(['/admin/rates/new']);
  }
}
