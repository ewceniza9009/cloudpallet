import { Component, OnInit, inject, signal } from '@angular/core';
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
import { AdminApiService, Rate } from '../../admin-api.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { map, startWith, debounceTime, distinctUntilChanged } from 'rxjs';

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
  ],
  templateUrl: './rate-list.component.html',
  styleUrls: ['./rate-list.component.scss'],
})
export class RateListComponent implements OnInit {
  private adminApi = inject(AdminApiService);
  private http = inject(HttpClient);
  private router = inject(Router);

  rates = signal<Rate[]>([]);
  filteredRates = signal<Rate[]>([]);
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
    this.loadData();

    // Add debounce for better search performance
    this.searchControl.valueChanges.pipe(
      startWith(''),
      debounceTime(300),
      distinctUntilChanged(),
      map(value => this._filter(value || ''))
    ).subscribe(filtered => this.filteredRates.set(filtered));
  }

  loadData(): void {
    this.isLoading.set(true);
    this.http.get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`).subscribe(accounts => {
        accounts.forEach(acc => this.accountMap.set(acc.id, acc.name));
        this.accountMap.set('00000000-0000-0000-0000-000000000000', 'Default');
    });

    this.adminApi.getRates().subscribe({
      next: (data) => {
        this.rates.set(data);
        this.filteredRates.set(data);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Failed to load rates:', error);
        this.isLoading.set(false);
      }
    });
  }

  private _filter(value: string): Rate[] {
    const filterValue = value.toLowerCase();
    return this.rates().filter(rate =>
      this.getAccountName(rate.accountId).toLowerCase().includes(filterValue) ||
      rate.serviceType.toLowerCase().includes(filterValue) ||
      rate.tier.toLowerCase().includes(filterValue) ||
      rate.uom.toLowerCase().includes(filterValue)
    );
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
