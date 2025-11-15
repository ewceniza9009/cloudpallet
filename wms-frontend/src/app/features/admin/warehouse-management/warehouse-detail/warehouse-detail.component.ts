// ---- File: wms-frontend/src/app/features/admin/warehouse-management/warehouse-detail/warehouse-detail.component.ts ----

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialog } from '@angular/material/dialog';
import { filter, switchMap, EMPTY, catchError } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../../shared/confirmation-dialog/confirmation-dialog.component';
import { AdminSetupApiService, WarehouseDto, CreateWarehouseCommand, UpdateWarehouseCommand } from '../../admin-setup-api.service';

@Component({
  selector: 'app-warehouse-detail',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSlideToggleModule
  ],
  templateUrl: './warehouse-detail.component.html',
  styleUrls: ['./warehouse-detail.component.scss']
})
export class WarehouseDetailComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  warehouseForm: FormGroup;
  warehouseId = signal<string | null>(null);
  isEditMode = signal(false);
  isLoading = signal(true);
  isSaving = signal(false);
  isDeleting = signal(false);

  constructor() {
    this.warehouseForm = this.fb.group({
      name: ['', Validators.required],
      address: this.fb.group({
        street: ['', Validators.required],
        city: ['', Validators.required],
        state: ['', Validators.required],
        postalCode: ['', Validators.required],
        country: ['', Validators.required]
      }),
      contactPhone: ['', Validators.required],
      contactEmail: ['', [Validators.required, Validators.email]],
      operatingHours: [''],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (id && id !== 'new') {
          this.warehouseId.set(id);
          this.isEditMode.set(false);
          this.warehouseForm.disable();
          return this.adminSetupApi.getWarehouseById(id).pipe(
             catchError(() => {
                this.snackBar.open('Warehouse not found.', 'Close', { duration: 3000 });
                this.router.navigate(['/setup/warehouses']);
                return EMPTY;
             })
          );
        } else {
          this.warehouseId.set(null);
          this.isEditMode.set(true);
          this.warehouseForm.enable();
          this.isLoading.set(false);
          return EMPTY;
        }
      })
    ).subscribe(warehouse => {
      if (warehouse) {
        this.warehouseForm.patchValue(warehouse);
      }
      this.isLoading.set(false);
    });
  }

  toggleEditMode(isEditing: boolean): void {
    this.isEditMode.set(isEditing);
    if (isEditing) {
      this.warehouseForm.enable();
    } else {
      this.warehouseForm.disable();
      // Revert changes
      this.adminSetupApi.getWarehouseById(this.warehouseId()!).subscribe(data => this.warehouseForm.patchValue(data));
    }
  }

  save(): void {
    if (this.warehouseForm.invalid) {
       this.snackBar.open('Please fill in all required fields.', 'Close', { duration: 3000 });
       this.warehouseForm.markAllAsTouched();
       return;
    }

    this.isSaving.set(true);
    const formValue = this.warehouseForm.getRawValue(); // Get values from disabled controls too

    if (this.warehouseId()) {
      const command: UpdateWarehouseCommand = { id: this.warehouseId()!, ...formValue };
      this.adminSetupApi.updateWarehouse(this.warehouseId()!, command).subscribe({
        next: () => this.handleSaveSuccess('Warehouse updated successfully.'),
        error: (err: any) => this.handleSaveError(err)
      });
    } else {
      const command: CreateWarehouseCommand = formValue;
      this.adminSetupApi.createWarehouse(command).subscribe({
        next: (newId) => {
          this.handleSaveSuccess('Warehouse created successfully.');
          this.router.navigate(['/setup/warehouses/detail', newId], { replaceUrl: true });
        },
        error: (err: any) => this.handleSaveError(err)
      });
    }
  }

  private handleSaveSuccess(message: string): void {
    this.isSaving.set(false);
    this.isEditMode.set(false);
    this.warehouseForm.disable();
    this.snackBar.open(message, 'OK', { duration: 3000 });
    this.router.navigate(['/setup/warehouses']); // Navigate back to list view on save
  }

  private handleSaveError(err: any): void {
    this.isSaving.set(false);
    this.snackBar.open(`Error: ${err.error?.title || 'Failed to save warehouse.'}`, 'Close');
  }

  delete(): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Warehouse',
        message: `Are you sure you want to delete "${this.warehouseForm.value.name}"? This action cannot be undone.`
      }
    });

    dialogRef.afterClosed().pipe(filter(result => result === true)).subscribe(() => {
      this.isDeleting.set(true);
      this.adminSetupApi.deleteWarehouse(this.warehouseId()!).subscribe({
        next: () => {
          this.snackBar.open('Warehouse deleted successfully.', 'OK', { duration: 3000 });
          this.router.navigate(['/setup/warehouses']);
        },
        error: (err: any) => {
          this.snackBar.open(`Error: ${err.error?.title || 'Failed to delete warehouse.'}`, 'Close');
          this.isDeleting.set(false);
        }
      });
    });
  }

  back(): void {
    this.router.navigate(['/setup/warehouses']);
  }

  // Helper to access nested address group
  get addressFormGroup(): FormGroup {
    return this.warehouseForm.get('address') as FormGroup;
  }
}
