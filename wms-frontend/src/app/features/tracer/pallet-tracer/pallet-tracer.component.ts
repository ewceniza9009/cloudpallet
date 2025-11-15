import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe, TitleCasePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  InventoryApiService,
  PalletMovementDto,
} from '../../inventory/inventory-api.service';

@Component({
  selector: 'app-pallet-tracer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DatePipe,
    TitleCasePipe,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './pallet-tracer.component.html',
  styleUrls: ['./pallet-tracer.component.scss'],
})
export class PalletTracerComponent {
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);

  searchControl = new FormControl('', { nonNullable: true });
  isLoading = signal(false);
  searched = signal(false);
  palletHistory = signal<PalletMovementDto[]>([]);

  onSearch(event?: Event): void {
    if (event) {
      event.preventDefault();
    }

    const searchValue = this.searchControl.value.trim();
    if (!searchValue) {
      this.snackBar.open('Please enter a pallet barcode to search', 'Close', {
        duration: 3000,
      });
      return;
    }

    this.isLoading.set(true);
    this.searched.set(true);
    this.palletHistory.set([]);

    this.inventoryApi.getPalletHistory(searchValue).subscribe({
      next: (data) => {
        this.palletHistory.set(data);
        if (data.length === 0) {
          this.snackBar.open(
            `No history found for pallet: ${searchValue}`,
            'Close',
            {
              duration: 5000,
              panelClass: ['snackbar-warning'],
            }
          );
        }
        this.isLoading.set(false);
      },
      error: (error) => {
        this.snackBar.open(
          error.status === 404
            ? `Pallet "${searchValue}" not found in the system`
            : 'An error occurred while fetching pallet history. Please try again.',
          'Close',
          { duration: 5000 }
        );
        this.isLoading.set(false);
      },
    });
  }

  getIconForEventType(eventType: string): string {
    switch (eventType.toLowerCase()) {
      case 'received':
        return 'inventory_2';
      case 'put away':
        return 'move_to_inbox';
      case 'transferred':
        return 'compare_arrows';
      case 'picked':
        return 'inventory';
      case 'shipped':
        return 'local_shipping';
      case 'created':
        return 'add_circle_outline';
      case 'adjusted':
        return 'tune';
      case 'blasting':
        return 'ac_unit';
      case 'repack':
        return 'transform';
      case 'kitting':
        return 'transform';
      default:
        return 'help_outline';
    }
  }

  getColorClassForEventType(eventType: string): string {
    switch (eventType.toLowerCase()) {
      case 'received':
      case 'put away':
      case 'created':
        return 'event-inbound';
      case 'transferred':
      case 'adjusted':
      case 'blasting':
      case 'repack':
        return 'event-internal';
      case 'picked':
      case 'shipped':
        return 'event-outbound';
      default:
        return 'event-default';
    }
  }
}
