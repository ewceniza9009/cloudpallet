import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { PickingApiService, PickListGroupDto, PickItem, ConfirmPickByScanRequest, ConfirmPickRequest } from '../../picking/picking-api.service';
import { ScanConfirmationDialogComponent, ScanDialogData } from '../../picking/scan-confirmation-dialog/scan-confirmation-dialog.component';
import { filter } from 'rxjs';

@Component({
    selector: 'app-mobile-picking',
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatChipsModule,
        MatSnackBarModule,
        MatDialogModule,
        MatProgressSpinnerModule,
        MatFormFieldModule,
        MatInputModule,
        FormsModule
    ],
    templateUrl: './mobile-picking.component.html',
    styleUrls: ['./mobile-picking.component.scss']
})
export class MobilePickingComponent implements OnInit {
    private pickingApi = inject(PickingApiService);
    private snackBar = inject(MatSnackBar);
    private dialog = inject(MatDialog);

    pickListGroups = signal<PickListGroupDto[]>([]);
    isLoading = signal(true);
    showSearch = signal(false);
    searchQuery = '';
    toastMsg = signal<string | null>(null);

    // Track expanded groups for accordion-like behavior
    expandedGroups = signal<Set<string>>(new Set());

    ngOnInit(): void {
        this.loadPickList();
    }

    loadPickList(): void {
        this.isLoading.set(true);
        this.pickingApi.getPickList().subscribe({
            next: (data) => {
                this.pickListGroups.set(data);
                this.isLoading.set(false);
                // Auto-expand first group if exists
                if (data.length > 0) {
                    this.toggleGroup(data[0].accountId);
                }
            },
            error: (err) => {
                console.error('Failed to load pick list', err);
                this.snackBar.open('Could not load pick list.', 'Close');
                this.isLoading.set(false);
            },
        });
    }

    toggleGroup(accountId: string): void {
        const current = this.expandedGroups();
        const newSet = new Set(current);
        if (newSet.has(accountId)) {
            newSet.delete(accountId);
        } else {
            newSet.add(accountId);
        }
        this.expandedGroups.set(newSet);
    }

    isGroupExpanded(accountId: string): boolean {
        return this.expandedGroups().has(accountId);
    }

    openScanDialog(item: PickItem): void {
        const dialogRef = this.dialog.open<ScanConfirmationDialogComponent, ScanDialogData>(
            ScanConfirmationDialogComponent,
            {
                width: '90vw', // Mobile full width
                maxWidth: '400px',
                data: {
                    pickId: item.pickId,
                    material: item.material,
                    location: item.location,
                },
            }
        );

        dialogRef.afterClosed().pipe(filter((result) => !!result)).subscribe((result) => {
            const request: ConfirmPickByScanRequest = {
                pickTransactionId: item.pickId,
                scannedLocationCode: result.scannedLocationCode,
                scannedLpn: result.scannedLpn,
                actualWeight: result.actualWeight,
            };

            this.pickingApi.confirmPickByScan(request).subscribe({
                next: () => this.updateItemStatus(item.pickId, 'Confirmed'),
                error: (err) =>
                    this.snackBar.open(
                        `Scan Confirmation Failed: ${err.error?.detail || err.error?.title || 'Check console.'}`,
                        'Close',
                        { duration: 5000 }
                    ),
            });
        });
    }

    markAsShort(item: PickItem): void {
        if (!confirm('Mark this item as SHORT?')) return;

        const request: ConfirmPickRequest = {
            pickTransactionId: item.pickId,
            newStatus: 'Short',
        };

        this.pickingApi.confirmPickManually(request).subscribe({
            next: () => this.updateItemStatus(item.pickId, 'Short'),
            error: (err) =>
                this.snackBar.open(
                    `Failed to mark as short: ${err.error?.title || 'Unknown error'}`,
                    'Close',
                    { duration: 5000 }
                ),
        });
    }

    startGeneralScan(): void {
        this.toastMsg.set('CAMERA COMPONENT MISSING. RUN: npm install @zxing/ngx-scanner');
        setTimeout(() => this.toastMsg.set(null), 5000);
    }

    private updateItemStatus(pickId: string, newStatus: 'Confirmed' | 'Short'): void {
        this.pickListGroups.update((groups) =>
            groups.map((g) => ({
                ...g,
                items: g.items.map((item) =>
                    item.pickId === pickId ? { ...item, status: newStatus } : item
                ),
            }))
        );
        this.snackBar.open(`Item marked as ${newStatus}.`, 'OK', { duration: 2000 });
    }
}
// Refreshed
