import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { filter, debounceTime, switchMap, tap } from 'rxjs';
import {
  InventoryApiService,
  StoredPalletDetailDto,
  PalletLineItemDto,
} from '../inventory-api.service';

import { TransferItemsDialogComponent } from '../transfer-items-dialog/transfer-items-dialog.component';
import { ManualPutawayDialogComponent } from '../../putaway/manual-putaway-dialog/manual-putaway-dialog.component';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog.component';
import {
  RecordLabelingDialogComponent,
  RecordLabelingDialogData,
  RecordLabelingDialogResult,
} from '../record-labeling-dialog/record-labeling-dialog.component';
import {
  StartQuarantineDialogComponent,
  StartQuarantineDialogData,
  StartQuarantineDialogResult,
} from '../start-quarantine-dialog/start-quarantine-dialog.component';
import {
  CompleteFumigationDialogComponent,
  CompleteFumigationDialogData,
  CompleteFumigationDialogResult,
} from '../complete-fumigation-dialog/complete-fumigation-dialog.component';

import { SearchPalletDialogComponent } from '../search-pallet-dialog/search-pallet-dialog.component';

@Component({
  selector: 'app-inventory-manager',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatListModule,
    MatDividerModule,
    DecimalPipe,
    MatTooltipModule,
    MatMenuModule,
  ],
  templateUrl: './inventory-manager.component.html',
  styleUrls: ['./inventory-manager.component.scss'],
})
export class InventoryManagerComponent implements OnInit {
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  isLoading = signal(false);
  searchControl = new FormControl('');
  foundPallet = signal<StoredPalletDetailDto | null>(null);

  private fullInventoryDetails = signal<any[]>([]);

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(500),
        filter((value) => !!value && value.length > 5),
        tap(() => {
          this.isLoading.set(true);
          this.foundPallet.set(null);
          this.fullInventoryDetails.set([]);
        }),
        switchMap((barcode) => this.searchPalletByBarcode(barcode!))
      )
      .subscribe((pallet) => {
        this.foundPallet.set(pallet);
        if (pallet) {
          this.loadFullInventoryDetails(pallet.lines.map((l) => l.inventoryId));
        } else {
          this.isLoading.set(false);
        }
      });
  }

  loadFullInventoryDetails(inventoryIds: string[]): void {
    this.fullInventoryDetails.set(
      this.foundPallet()!.lines.map((line) => ({
        ...line,
        status: 'Available',
      }))
    );
    this.isLoading.set(false);
  }

  searchPalletByBarcode(
    barcode: string
  ): Promise<StoredPalletDetailDto | null> {
    return new Promise((resolve) => {
      this.inventoryApi.getStoredPalletsByRoom().subscribe({
        next: (rooms) => {
          const pallet = rooms
            .flatMap((r) => r.pallets)
            .find((p) =>
              p.palletBarcode.toUpperCase().includes(barcode.toUpperCase())
            );
          resolve(pallet || null);
        },
        error: () => resolve(null),
      });
    });
  }

  openSearchDialog(): void {
    const dialogRef = this.dialog.open(SearchPalletDialogComponent, {
      width: '700px',
      maxWidth: '95vw',
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result) => !!result))
      .subscribe((selectedBarcode) => {
        this.searchControl.setValue(selectedBarcode);
      });
  }

  getItemStatus(inventoryId: string): 'Available' | 'Quarantined' | 'Unknown' {
    const item = this.fullInventoryDetails().find(
      (i) => i.inventoryId === inventoryId
    );
    return item?.status || 'Available';
  }
  getItemStatusClass(inventoryId: string): string {
    const status = this.getItemStatus(inventoryId);
    if (status === 'Quarantined') return 'status-quarantined';
    return 'status-available';
  }
  getItemStatusIcon(inventoryId: string): string {
    const status = this.getItemStatus(inventoryId);
    if (status === 'Quarantined') return 'gpp_bad';
    return 'widgets';
  }
  private setLocalItemStatus(
    inventoryId: string,
    status: 'Available' | 'Quarantined'
  ): void {
    this.fullInventoryDetails.update((items) =>
      items.map((item) =>
        item.inventoryId === inventoryId ? { ...item, status: status } : item
      )
    );
  }

  onMove(pallet: StoredPalletDetailDto): void {
    const dialogRef = this.dialog.open(ManualPutawayDialogComponent);
    dialogRef
      .afterClosed()
      .pipe(filter((result) => !!result))
      .subscribe((selectedLocationId: string) => {
        this.inventoryApi
          .executeTransfer({
            palletId: pallet.palletId,
            sourceLocationId: pallet.currentLocationId,
            destinationLocationId: selectedLocationId,
          })
          .subscribe({
            next: () => {
              this.snackBar.open(
                `Pallet ${pallet.palletBarcode} moved successfully!`,
                'OK',
                { duration: 3000 }
              );
              this.searchControl.setValue('');
              this.foundPallet.set(null);
            },
            error: (err: any) =>
              this.snackBar.open(
                `Error: ${err.error?.title || 'Could not complete transfer.'}`,
                'Close'
              ),
          });
      });
  }

  onBlastFreeze(pallet: StoredPalletDetailDto): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Confirm Blast Freeze',
        message: `This will record a billable Blast Freezing service for pallet ${pallet.palletBarcode}. This action cannot be undone. Continue?`,
        confirmButtonText: 'Confirm',
      },
    });
    dialogRef
      .afterClosed()
      .pipe(filter((result) => result === true))
      .subscribe(() => {
        this.inventoryApi.recordBlastFreeze(pallet.palletId).subscribe({
          next: () =>
            this.snackBar.open(
              'Blast freezing service recorded successfully.',
              'OK',
              { duration: 3000 }
            ),
          error: (err: any) =>
            this.snackBar.open(
              `Error: ${err.error?.title || 'Could not record service.'}`,
              'Close'
            ),
        });
      });
  }

  onLabelPallet(pallet: StoredPalletDetailDto): void {
    const dialogRef = this.dialog.open<
      RecordLabelingDialogComponent,
      RecordLabelingDialogData,
      RecordLabelingDialogResult
    >(RecordLabelingDialogComponent, {
      data: { targetName: `Pallet ${pallet.palletBarcode}` },
    });
    dialogRef
      .afterClosed()
      .pipe(filter((result): result is RecordLabelingDialogResult => !!result))
      .subscribe((result) => {
        this.inventoryApi
          .recordLabeling({
            targetId: pallet.palletId,
            targetType: 'Pallet',
            labelType: result.labelType,
            quantityLabeled: 1,
          })
          .subscribe({
            next: () =>
              this.snackBar.open(
                `${result.labelType} labeling recorded for pallet.`,
                'OK',
                { duration: 3000 }
              ),
            error: (err: any) =>
              this.snackBar.open(
                `Error: ${err.error?.title || 'Could not record labeling.'}`,
                'Close'
              ),
          });
      });
  }

  startTransfer(pallet: StoredPalletDetailDto): void {
    const dialogRef = this.dialog.open(TransferItemsDialogComponent, {
      data: {
        sourcePalletId: pallet.palletId,
        sourcePalletBarcode: pallet.palletBarcode,
        sourceInventoryLines: pallet.lines,
      },
      width: '600px',
    });
    dialogRef
      .afterClosed()
      .pipe(filter((result) => !!result))
      .subscribe(() => {
        this.snackBar.open(
          `Material successfully transferred to new pallet.`,
          'OK',
          { duration: 5000 }
        );
        this.searchControl.setValue('');
        this.foundPallet.set(null);
      });
  }

  onLabelItem(line: PalletLineItemDto): void {
    const dialogRef = this.dialog.open<
      RecordLabelingDialogComponent,
      RecordLabelingDialogData,
      RecordLabelingDialogResult
    >(RecordLabelingDialogComponent, {
      data: { targetName: `Item ${line.barcode}` },
    });
    dialogRef
      .afterClosed()
      .pipe(filter((result): result is RecordLabelingDialogResult => !!result))
      .subscribe((result) => {
        this.inventoryApi
          .recordLabeling({
            targetId: line.inventoryId,
            targetType: 'InventoryItem',
            labelType: result.labelType,
            quantityLabeled: line.quantity,
          })
          .subscribe({
            next: () =>
              this.snackBar.open(
                `${result.labelType} labeling recorded for item.`,
                'OK',
                { duration: 3000 }
              ),
            error: (err: any) =>
              this.snackBar.open(
                `Error: ${err.error?.title || 'Could not record labeling.'}`,
                'Close'
              ),
          });
      });
  }

  onStartQuarantine(line: PalletLineItemDto): void {
    const dialogRef = this.dialog.open<
      StartQuarantineDialogComponent,
      StartQuarantineDialogData,
      StartQuarantineDialogResult
    >(StartQuarantineDialogComponent, {
      data: { targetName: line.barcode },
    });
    dialogRef
      .afterClosed()
      .pipe(filter((result): result is StartQuarantineDialogResult => !!result))
      .subscribe((result) => {
        this.inventoryApi
          .startQuarantine({
            inventoryId: line.inventoryId,
            reason: result.reason,
          })
          .subscribe({
            next: () => {
              this.snackBar.open(
                `Item ${line.barcode} is now quarantined.`,
                'OK',
                { duration: 3000 }
              );
              this.setLocalItemStatus(line.inventoryId, 'Quarantined');
            },
            error: (err: any) =>
              this.snackBar.open(
                `Error: ${err.error?.title || 'Could not quarantine item.'}`,
                'Close'
              ),
          });
      });
  }

  onCompleteFumigation(line: PalletLineItemDto): void {
    const dialogRef = this.dialog.open<
      CompleteFumigationDialogComponent,
      CompleteFumigationDialogData,
      CompleteFumigationDialogResult
    >(CompleteFumigationDialogComponent, {
      data: { targetName: line.barcode },
    });
    dialogRef
      .afterClosed()
      .pipe(
        filter((result): result is CompleteFumigationDialogResult => !!result)
      )
      .subscribe((result) => {
        this.inventoryApi
          .completeFumigation({
            inventoryId: line.inventoryId,
            durationHours: result.durationHours,
          })
          .subscribe({
            next: () => {
              this.snackBar.open(
                `Item ${line.barcode} has been released from quarantine.`,
                'OK',
                { duration: 3000 }
              );
              this.setLocalItemStatus(line.inventoryId, 'Available');
            },
            error: (err: any) =>
              this.snackBar.open(
                `Error: ${err.error?.title || 'Could not release item.'}`,
                'Close'
              ),
          });
      });
  }
}
