import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { filter, from } from 'rxjs';
import { ShippingApiService, ShippableGroupDto } from './shipping-api.service';
import { SelectAppointmentDialogComponent } from './select-appointment-dialog/select-appointment-dialog.component';

@Component({
  selector: 'app-shipping',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './shipping.component.html',
  styleUrls: ['./shipping.component.scss'],
})
export class ShippingComponent implements OnInit {
  private shippingApi = inject(ShippingApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  groups = signal<ShippableGroupDto[]>([]);
  isLoading = signal(true);

  ngOnInit(): void {
    this.loadShippableGroups();
  }

  loadShippableGroups(): void {
    this.isLoading.set(true);
    this.shippingApi.getShippableGroups().subscribe({
      next: (data) => {
        this.groups.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open(
          'Failed to load groups ready for shipping.',
          'Close'
        );
        this.isLoading.set(false);
      },
    });
  }

  onShip(group: ShippableGroupDto): void {
    const dialogRef = this.dialog.open(SelectAppointmentDialogComponent, {
      width: '500px',
      data: { group },
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result) => !!result))
      .subscribe((result) => {
        this.shippingApi
          .shipGoods({
            appointmentId: result.appointmentId,
            shipmentNumber: `SHIP-${Date.now()}`,
            pickTransactionIds: group.pickTransactionIds,
          })
          .subscribe({
            next: () => {
              this.snackBar.open(
                `Order for ${group.accountName} marked as shipped!`,
                'OK',
                { duration: 4000 }
              );
              this.loadShippableGroups(); // Refresh the list
            },
            error: (err) =>
              this.snackBar.open(
                `Error: ${err.error?.title || 'Could not complete shipment.'}`,
                'Close'
              ),
          });
      });
  }
}
