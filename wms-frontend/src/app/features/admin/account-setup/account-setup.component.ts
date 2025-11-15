// ---- File: wms-frontend/src/app/features/admin/account-setup/account-setup.component.ts [COMPLETE & FIXED] ----

import { Component, OnInit, inject, signal, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common'; // <-- Removed DecimalPipe
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
import { MatTabsModule } from '@angular/material/tabs';
import { MatSelectModule } from '@angular/material/select';
import { AdminSetupApiService, AccountDto, AccountDetailDto, CreateAccountCommand, UpdateAccountCommand, GetAccountsQuery } from '../admin-setup-api.service';
import { ConfirmationDialogComponent } from '../../../shared/confirmation-dialog/confirmation-dialog.component';
// --- FIX 1: Added 'filter' import ---
import { merge, startWith, switchMap, map, Subject, debounceTime, catchError, of, finalize, tap, filter } from 'rxjs';

@Component({
  selector: 'app-account-setup',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTableModule, MatDividerModule,
    MatSlideToggleModule, MatTooltipModule, MatCheckboxModule, MatPaginatorModule, MatSortModule,
    MatTabsModule, MatSelectModule // <-- FIX 2: Removed DecimalPipe
  ],
  templateUrl: './account-setup.component.html',
  styleUrls: ['./account-setup.component.scss']
})
export class AccountSetupComponent implements OnInit, AfterViewInit {
  private fb = inject(FormBuilder);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  isLoadingList = signal(true);
  isSaving = signal(false);
  isLoadingDetails = signal(false);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  dataSource = new MatTableDataSource<AccountDto>([]);
  displayedColumns = ['name', 'phone', 'city', 'isActive'];
  resultsLength = signal(0);
  searchControl = new FormControl('');
  private searchTrigger = new Subject<void>();

  accountForm: FormGroup;
  editingAccountId = signal<string | null>(null);
  accountTypes: ('Direct' | 'ThreePL' | 'Vendor')[] = ['Direct', 'ThreePL', 'Vendor'];
  tempZones = ['Chilling', 'FrozenStorage', 'CoolStorage', 'DeepFrozenStorage', 'ULTStorage'];
  categories = signal<{id: string, name: string}[]>([]);

  constructor() {
    this.accountForm = this.fb.group({
      name: ['', Validators.required],
      typeId: ['Direct', Validators.required],
      categoryId: [null as string | null],
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
      preferredTempZone: [null as string | null],
      isPreferred: [false],
      creditLimit: [0, [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    this.accountForm.disable();
    // TODO: Load categories
    // this.http.get<...[]>(`${environment.apiUrl}/Lookups/account-categories`).subscribe(res => this.categories.set(res));
  }

  ngAfterViewInit(): void {
    this.sort.sortChange.subscribe(() => this.paginator.pageIndex = 0);

    merge(this.sort.sortChange, this.paginator.page, this.searchControl.valueChanges.pipe(debounceTime(400)), this.searchTrigger).pipe(
      startWith({}),
      tap(() => this.isLoadingList.set(true)),
      switchMap(() => {
        const query: GetAccountsQuery = {
          page: this.paginator.pageIndex + 1,
          pageSize: this.paginator.pageSize || 10,
          sortBy: this.sort.active || 'name',
          sortDirection: this.sort.direction || 'asc',
          searchTerm: this.searchControl.value || undefined
        };
        return this.adminSetupApi.getPagedAccounts(query).pipe(
          catchError(() => {
            this.snackBar.open('Failed to load accounts.', 'Close');
            return of(null);
          })
        );
      })
    ).subscribe(data => {
      this.isLoadingList.set(false);
      if (data) {
        this.resultsLength.set(data.totalCount);
        this.dataSource.data = data.items;

        if (!this.editingAccountId() && data.items.length > 0) {
          this.onSelectAccount(data.items[0]);
        } else if (data.items.length === 0) {
          this.clearSelection();
        }
      }
    });
  }

  refreshList(): void {
    this.searchTrigger.next();
  }

  onSelectAccount(account: AccountDto): void {
    if (this.editingAccountId() === account.id) {
        this.accountForm.enable();
        return;
    }

    this.isLoadingDetails.set(true);
    this.accountForm.disable();
    this.adminSetupApi.getAccountById(account.id).subscribe({
      next: (detail) => {
        this.editingAccountId.set(detail.id);
        this.accountForm.patchValue(detail);
        this.accountForm.enable();
        this.isLoadingDetails.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load account details.', 'Close');
        this.isLoadingDetails.set(false);
      }
    });
  }

  onSaveAccount(): void {
    if (this.accountForm.invalid) {
      this.snackBar.open('Please correct form errors. All fields with * are required.', 'Close');
      this.accountForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.accountForm.getRawValue();

    if (this.editingAccountId()) {
      const command: UpdateAccountCommand = { id: this.editingAccountId()!, ...formValue };
      this.adminSetupApi.updateAccount(command.id, command).subscribe({
        next: () => this.handleSaveSuccess('Account updated.'),
        error: (err) => this.handleSaveError(err)
      });
    } else {
      const command: CreateAccountCommand = formValue;
      this.adminSetupApi.createAccount(command).subscribe({
        next: () => this.handleSaveSuccess('Account created.'),
        error: (err) => this.handleSaveError(err)
      });
    }
  }

  onDeleteAccount(): void {
    if (!this.editingAccountId()) return;

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: { title: 'Delete Account', message: `Are you sure you want to delete "${this.accountForm.value.name}"?` }
    });

    // --- FIX 3: Type 'res' as boolean ---
    dialogRef.afterClosed().pipe(filter((res: boolean) => res === true)).subscribe(() => {
      this.isSaving.set(true);
      this.adminSetupApi.deleteAccount(this.editingAccountId()!).subscribe({
        next: () => {
          this.snackBar.open('Account deleted.', 'OK', { duration: 2000 });
          this.editingAccountId.set(null);
          this.refreshList();
          this.isSaving.set(false);
        },
        error: (err) => this.handleSaveError(err)
      });
    });
  }

  clearSelection(): void {
    this.editingAccountId.set(null);
    this.accountForm.reset({
      name: '',
      typeId: 'Direct',
      categoryId: null,
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
      preferredTempZone: null,
      isPreferred: false,
      creditLimit: 0
    });
    this.accountForm.enable();
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
}
