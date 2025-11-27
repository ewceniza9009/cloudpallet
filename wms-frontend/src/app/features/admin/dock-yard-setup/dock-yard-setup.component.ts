// ---- File: dock-yard-setup.component.ts ----

import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminSetupApiService, DockSetupDto, YardSpotSetupDto } from '../admin-setup-api.service';
import { WarehouseStateService } from '../../../core/services/warehouse-state.service';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog.component';
import { EditDockYardModalComponent, EditModalData } from '../../../shared/edit-dock-yard-modal/edit-dock-yard-modal.component';
import { filter, catchError, of } from 'rxjs';

@Component({
  selector: 'app-dock-yard-setup',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './dock-yard-setup.component.html',
  styleUrls: ['./dock-yard-setup.component.scss']
})
export class DockYardSetupComponent implements OnInit {
  private fb = inject(FormBuilder);
  private adminSetupApi = inject(AdminSetupApiService);
  private warehouseState = inject(WarehouseStateService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  isLoading = signal(true);
  isSavingDock = signal(false);
  isSavingYardSpot = signal(false);
  warehouseId = this.warehouseState.selectedWarehouseId;

  // --- Dock State ---
  docks = signal<DockSetupDto[]>([]);
  dockSearchControl = new FormControl('');
  dockColumns = ['name', 'actions'];
  filteredDocks = computed(() => {
    const filter = this.dockSearchControl.value?.toLowerCase() || '';
    return this.docks().filter(d => d.name.toLowerCase().includes(filter));
  });

  // --- Yard Spot State ---
  yardSpots = signal<YardSpotSetupDto[]>([]);
  yardSearchControl = new FormControl('');
  yardSpotColumns = ['spotNumber', 'status', 'actions'];
  filteredYardSpots = computed(() => {
    const filter = this.yardSearchControl.value?.toLowerCase() || '';
    return this.yardSpots().filter(ys => ys.spotNumber.toLowerCase().includes(filter));
  });

  constructor() {}

  ngOnInit(): void {
    if (!this.warehouseId()) {
      this.snackBar.open('Please select a warehouse first.', 'Close', { duration: 5000 });
      this.isLoading.set(false);
      return;
    }
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    this.adminSetupApi.getDockYardSetup(this.warehouseId()!).subscribe({
      next: (data) => {
        console.log('Data loaded successfully:', data);
        this.docks.set(data.docks || []);
        this.yardSpots.set(data.yardSpots || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading data:', err);
        this.snackBar.open('Failed to load dock and yard data.', 'Close');
        this.isLoading.set(false);
      }
    });
  }

  // --- Dock Methods ---
  onAddDock(): void {
    const dialogRef = this.dialog.open(EditDockYardModalComponent, {
      data: { type: 'dock' } as EditModalData,
      width: '450px'
    });

    dialogRef.afterClosed().pipe(filter(result => !!result)).subscribe(result => {
      this.isSavingDock.set(true);
      this.adminSetupApi.createDock({ warehouseId: this.warehouseId()!, name: result.name }).subscribe({
        next: () => this.handleSaveSuccess('Dock created successfully.'),
        error: (err) => this.handleSaveError(err, 'dock')
      });
    });
  }

  onEditDock(dock: DockSetupDto): void {
    const dialogRef = this.dialog.open(EditDockYardModalComponent, {
      data: { type: 'dock', item: dock } as EditModalData,
      width: '450px'
    });

    dialogRef.afterClosed().pipe(filter(result => !!result)).subscribe(result => {
      this.isSavingDock.set(true);
      this.adminSetupApi.updateDock(dock.id, { dockId: dock.id, name: result.name }).subscribe({
        next: () => this.handleSaveSuccess('Dock updated successfully.'),
        error: (err) => this.handleSaveError(err, 'dock')
      });
    });
  }

  onDeleteDock(dock: DockSetupDto): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: { title: 'Delete Dock', message: `Are you sure you want to delete dock "${dock.name}"?` }
    });
    dialogRef.afterClosed().pipe(filter(res => res === true)).subscribe(() => {
      this.adminSetupApi.deleteDock(dock.id).subscribe({
        next: () => this.handleSaveSuccess('Dock deleted successfully.'),
        error: (err) => this.handleSaveError(err, 'dock')
      });
    });
  }

  // --- Yard Spot Methods ---
  onAddYardSpot(): void {
    const dialogRef = this.dialog.open(EditDockYardModalComponent, {
      data: { type: 'yardSpot' } as EditModalData,
      width: '450px'
    });

    dialogRef.afterClosed().pipe(filter(result => !!result)).subscribe(result => {
      this.isSavingYardSpot.set(true);
      this.adminSetupApi.createYardSpot({
        warehouseId: this.warehouseId()!,
        spotNumber: result.spotNumber
      }).subscribe({
        next: () => this.handleSaveSuccess('Yard spot created successfully.'),
        error: (err) => this.handleSaveError(err, 'spot')
      });
    });
  }

  onEditYardSpot(spot: YardSpotSetupDto): void {
    const dialogRef = this.dialog.open(EditDockYardModalComponent, {
      data: { type: 'yardSpot', item: spot } as EditModalData,
      width: '450px'
    });

    dialogRef.afterClosed().pipe(filter(result => !!result)).subscribe(result => {
      this.isSavingYardSpot.set(true);
      this.adminSetupApi.updateYardSpot(spot.id, {
        yardSpotId: spot.id,
        spotNumber: result.spotNumber,
        isActive: result.isActive
      }).subscribe({
        next: () => this.handleSaveSuccess('Yard spot updated successfully.'),
        error: (err) => this.handleSaveError(err, 'spot')
      });
    });
  }

  onDeleteYardSpot(spot: YardSpotSetupDto): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: { title: 'Delete Yard Spot', message: `Are you sure you want to delete "${spot.spotNumber}"?` }
    });
    dialogRef.afterClosed().pipe(filter(res => res === true)).subscribe(() => {
      this.adminSetupApi.deleteYardSpot(spot.id).subscribe({
        next: () => this.handleSaveSuccess('Yard spot deleted successfully.'),
        error: (err) => this.handleSaveError(err, 'spot')
      });
    });
  }

  // --- Common Helpers ---
  private handleSaveSuccess(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 2000 });
    this.loadData(); // Reload all data
    this.isSavingDock.set(false);
    this.isSavingYardSpot.set(false);
  }

  private handleSaveError(err: any, type: 'dock' | 'spot'): void {
    this.snackBar.open(`Error: ${err.error?.title || 'Failed to save.'}`, 'Close');
    if (type === 'dock') this.isSavingDock.set(false);
    if (type === 'spot') this.isSavingYardSpot.set(false);
  }
}
