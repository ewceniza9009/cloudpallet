import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { filter } from 'rxjs';
import {
  InventoryApiService,
  PutawayTaskDto,
} from '../inventory/inventory-api.service';
import { ManualPutawayDialogComponent } from './manual-putaway-dialog/manual-putaway-dialog.component';
import { ConfirmationDialogComponent } from '../../shared/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'app-putaway-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatTooltipModule,
  ],
  templateUrl: './putaway-list.component.html',
  styleUrls: ['./putaway-list.component.scss'],
})
export class PutawayListComponent implements OnInit {
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  tasks = signal<PutawayTaskDto[]>([]);
  isLoading = signal(true);

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.isLoading.set(true);
    this.inventoryApi.getPutawayTasks().subscribe({
      next: (data) => {
        this.tasks.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load putaway tasks.', 'Close');
        this.isLoading.set(false);
      },
    });
  }

  onBlastFreeze(task: PutawayTaskDto): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Confirm Blast Freeze',
        message: `This will record a billable Blast Freezing service for pallet ${task.palletBarcode}. This action cannot be undone. Continue?`,
        confirmButtonText: 'Confirm',
      },
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result) => result === true))
      .subscribe(() => {
        this.inventoryApi.recordBlastFreeze(task.palletId).subscribe({
          next: () => {
            this.snackBar.open(
              'Blast freezing service recorded successfully.',
              'OK',
              { duration: 3000 }
            );
          },

          error: (err: any) =>
            this.snackBar.open(
              `Error: ${err.error?.title || 'Could not record service.'}`,
              'Close'
            ),
        });
      });
  }

  onPutaway(task: PutawayTaskDto): void {
    this.executePutaway(task.palletId, task.suggestedLocationId);
  }

  onManualPutaway(task: PutawayTaskDto): void {
    const dialogRef = this.dialog.open(ManualPutawayDialogComponent);

    dialogRef
      .afterClosed()
      .pipe(filter((result) => !!result))
      .subscribe((selectedLocationId: string) => {
        this.executePutaway(task.palletId, selectedLocationId);
      });
  }

  private executePutaway(
    palletId: string,
    destinationLocationId: string
  ): void {
    this.inventoryApi
      .executePutaway(palletId, destinationLocationId)
      .subscribe({
        next: () => {
          this.snackBar.open(`Pallet put away successfully!`, 'OK', {
            duration: 3000,
          });
          this.tasks.update((currentTasks) =>
            currentTasks.filter((t) => t.palletId !== palletId)
          );
        },
        error: (err: any) => {
          this.snackBar.open(
            `Error: ${err.error?.title || 'Could not complete putaway.'}`,
            'Close',
            { duration: 5000 }
          );
        },
      });
  }
}
