// ---- File: wms-frontend/src/app/features/admin/supplier-setup/supplier-setup.component.ts [MODIFIED] ----

import { Component, OnInit, inject, signal, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common'; // Removed DecimalPipe
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatDividerModule } from '@angular/material/divider';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTabsModule } from '@angular/material/tabs'; // <-- *** ADD THIS IMPORT ***
import { AdminSetupApiService, SupplierDto, SupplierDetailDto, CreateSupplierCommand, UpdateSupplierCommand, GetSuppliersQuery } from '../admin-setup-api.service';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog.component';
import { merge, startWith, switchMap, map, Subject, debounceTime, catchError, of, finalize, tap, filter } from 'rxjs';

@Component({
  selector: 'app-supplier-setup',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule, MatDividerModule,
    MatSlideToggleModule, MatTooltipModule, MatCheckboxModule, MatPaginatorModule, MatSortModule,
    MatTabsModule // <-- *** ADD THE MODULE HERE ***
  ],
  templateUrl: './supplier-setup.component.html',
  styleUrls: ['./supplier-setup.component.scss']
})
export class SupplierSetupComponent implements OnInit, AfterViewInit {
  // ... (The rest of your component logic is 100% correct) ...
  // ... (constructor, ngOnInit, ngAfterViewInit, loadSuppliers, etc. all stay the same) ...

  private fb = inject(FormBuilder);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  isLoadingList = signal(true);
  isSaving = signal(false);
  isLoadingDetails = signal(false);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  dataSource = new MatTableDataSource<SupplierDto>([]);
  displayedColumns = ['name', 'phone', 'city', 'isActive'];
  resultsLength = signal(0);
  searchControl = new FormControl('');
  private searchTrigger = new Subject<void>();

  supplierForm: FormGroup;
  editingSupplierId = signal<string | null>(null);

  constructor() {
    this.supplierForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      taxId: ['', Validators.required],
      isActive: [true],
      contactName: [''],
      phone: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      address: this.fb.group({
        street: ['', Validators.required],
        city: ['', Validators.required],
        state: ['', Validators.required],
        postalCode: ['', Validators.required],
        country: ['', Validators.required]
      }),
      leadTimeDays: [0, [Validators.required, Validators.min(0)]],
      certificationColdChain: [false],
      paymentTerms: ['N/A', Validators.required],
      currencyCode: ['PHP', Validators.required],
      creditLimit: [0, [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    this.supplierForm.disable();
  }

  ngAfterViewInit(): void {
    this.sort.sortChange.subscribe(() => this.paginator.pageIndex = 0);

    merge(this.sort.sortChange, this.paginator.page, this.searchControl.valueChanges.pipe(debounceTime(400)), this.searchTrigger).pipe(
      startWith({}),
      tap(() => this.isLoadingList.set(true)),
      switchMap(() => {
        const query: GetSuppliersQuery = {
          page: this.paginator.pageIndex + 1,
          pageSize: this.paginator.pageSize || 10,
          sortBy: this.sort.active || 'name',
          sortDirection: this.sort.direction || 'asc',
          searchTerm: this.searchControl.value || undefined
        };
        return this.adminSetupApi.getAllSuppliers(query).pipe(
          catchError(() => {
            this.snackBar.open('Failed to load suppliers.', 'Close');
            return of(null);
          })
        );
      })
    ).subscribe(data => {
      this.isLoadingList.set(false);
      if (data) {
        this.resultsLength.set(data.totalCount);
        this.dataSource.data = data.items;

        if (!this.editingSupplierId() && data.items.length > 0) {
          this.onSelectSupplier(data.items[0]);
        } else if (data.items.length === 0) {
          this.clearSelection();
        }
      }
    });
  }

  refreshList(): void {
    this.searchTrigger.next();
  }

  onSelectSupplier(supplier: SupplierDto): void {
    if (this.editingSupplierId() === supplier.id) {
        this.supplierForm.enable();
        return;
    }

    this.isLoadingDetails.set(true);
    this.supplierForm.disable();
    this.adminSetupApi.getSupplierById(supplier.id).subscribe({
      next: (detail) => {
        this.editingSupplierId.set(detail.id);
        this.supplierForm.patchValue(detail);
        this.supplierForm.enable();
        this.isLoadingDetails.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load supplier details.', 'Close');
        this.isLoadingDetails.set(false);
      }
    });
  }

  onSaveSupplier(): void {
    if (this.supplierForm.invalid) {
      this.snackBar.open('Please correct form errors. All fields with * are required.', 'Close');
      this.supplierForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.supplierForm.getRawValue();

    if (this.editingSupplierId()) {
      const command: UpdateSupplierCommand = { id: this.editingSupplierId()!, ...formValue };
      this.adminSetupApi.updateSupplier(command.id, command).subscribe({
        next: () => this.handleSaveSuccess('Supplier updated.'),
        error: (err) => this.handleSaveError(err)
      });
    } else {
      const command: CreateSupplierCommand = formValue;
      this.adminSetupApi.createSupplier(command).subscribe({
        next: () => this.handleSaveSuccess('Supplier created.'),
        error: (err) => this.handleSaveError(err)
      });
    }
  }

  onDeleteSupplier(): void {
    if (!this.editingSupplierId()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: { title: 'Delete Supplier', message: `Are you sure you want to delete "${this.supplierForm.value.name}"?` }
    });
    dialogRef.afterClosed().pipe(filter(res => res === true)).subscribe(() => {
      this.isSaving.set(true);
      this.adminSetupApi.deleteSupplier(this.editingSupplierId()!).subscribe({
        next: () => {
          this.snackBar.open('Supplier deleted.', 'OK', { duration: 2000 });
          this.editingSupplierId.set(null);
          this.refreshList();
          this.isSaving.set(false);
        },
        error: (err) => this.handleSaveError(err)
      });
    });
  }

  clearSelection(): void {
    this.editingSupplierId.set(null);
    this.supplierForm.reset({
      name: '',
      description: '',
      taxId: '',
      isActive: true,
      contactName: '',
      phone: '',
      email: '',
      address: { street: '', city: '', state: '', postalCode: '', country: '' },
      leadTimeDays: 0,
      certificationColdChain: false,
      paymentTerms: 'N/A',
      currencyCode: 'PHP',
      creditLimit: 0
    });
    this.supplierForm.enable();
  }

  private handleSaveSuccess(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 2000 });
    this.refreshList();
    this.clearSelection();
    this.isSaving.set(false);
  }

  private handleSaveError(err: any): void {
    this.snackBar.open(`Error: ${err.error?.title || 'Failed to save.'}`, 'Close');
    this.isSaving.set(false);
  }

  getDisplayName(item: { name: string } | string | null): string {
    if (!item || typeof item === 'string') return item || '';
    return item.name;
  }

  private _filter(value: string | { name: string } | null, options: any[], isMaterial: boolean = false): any[] {
    const filterValue = (typeof value === 'string' ? value : (value as { name: string })?.name || '').toLowerCase();
    return options.filter(option =>
        option.name.toLowerCase().includes(filterValue) ||
        (isMaterial && (option.sku && option.sku.toLowerCase().includes(filterValue)))
    );
  }
}
