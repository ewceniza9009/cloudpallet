import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router, RouterModule, NavigationEnd } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { InventoryApiService, ReceivingSessionDto } from '../inventory/inventory-api.service';
import { Subscription, filter } from 'rxjs';

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
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './receiving.component.html',
  styleUrls: ['./receiving.component.scss']
})
export class ReceivingComponent implements OnInit, OnDestroy {
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);
  private router = inject(Router);

  sessions = signal<ReceivingSessionDto[]>([]);
  isLoading = signal(true);
  displayedColumns = ['supplierName', 'licensePlate', 'status', 'palletCount', 'timestamp', 'actions'];

  private routerSubscription!: Subscription;

  ngOnInit(): void {
    this.loadSessions();

    this.routerSubscription = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd && event.url === '/receiving')
    ).subscribe(() => {
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
    this.inventoryApi.getReceivingSessions().subscribe({
      next: (data) => {
        this.sessions.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load receiving sessions.', 'Close');
        this.isLoading.set(false);
      }
    });
  }

  startNewSession(): void {
    this.router.navigate(['/receiving/new']);
  }
}
