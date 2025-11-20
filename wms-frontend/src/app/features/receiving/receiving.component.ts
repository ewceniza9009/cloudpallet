// ---- File: receiving.component.ts ----

import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator'; // <--- Added Import
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { InventoryApiService, ReceivingSessionDto } from '../inventory/inventory-api.service';

@Component({
  selector: 'app-receiving',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    DatePipe,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatPaginatorModule, // <--- Added Module
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './receiving.component.html',
  styleUrls: ['./receiving.component.scss'],
})
export class ReceivingComponent implements OnInit, OnDestroy {
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);
  private router = inject(Router);

  // Data Signals
  sessions = signal<ReceivingSessionDto[]>([]);
  totalCount = signal<number>(0); // <--- Added for Paginator length
  isLoading = signal(true);

  // Pagination Signals (Default to page 0 for Material, Size 10)
  pageIndex = signal(0);
  pageSize = signal(10);

  displayedColumns = [
    'supplierName',
    'licensePlate',
    'status',
    'palletCount',
    'timestamp',
    'actions',
  ];

  private routerSubscription!: Subscription;

  ngOnInit(): void {
    this.loadSessions();

    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd && event.url === '/receiving'))
      .subscribe(() => {
        // Reset pagination on full navigation reload if desired, or keep current state
        this.loadSessions();
      });
  }

  ngOnDestroy(): void {
    if (this.routerSubscription) {
      this.routerSubscription.unsubscribe();
    }
  }

  loadSessions(): void {
    this.isLoading.set(true);

    // Backend expects 1-based index, Material provides 0-based
    const apiPage = this.pageIndex() + 1;

    this.inventoryApi.getReceivingSessions(apiPage, this.pageSize()).subscribe({
      next: result => {
        // Result is now PagedResult<T>
        this.sessions.set(result.items);
        this.totalCount.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load receiving sessions.', 'Close');
        this.isLoading.set(false);
      },
    });
  }

  // Handle Paginator Events
  onPageChange(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadSessions();
  }

  startNewSession(): void {
    this.router.navigate(['/receiving/new']);
  }
}
