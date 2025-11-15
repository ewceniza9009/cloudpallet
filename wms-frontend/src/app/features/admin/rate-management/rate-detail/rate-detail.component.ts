// ---- File: wms-frontend/src/app/features/admin/rate-management/rate-detail/rate-detail.component.ts [COMPLETE] ----

import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { AdminApiService, CreateRateCommand, RateUom, ServiceType, UpdateRateCommand, RateDto } from '../../admin-api.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { filter, switchMap } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../../shared/confirmation-dialog/confirmation-dialog.component';
import { MatDialog } from '@angular/material/dialog';

interface AccountDto { id: string; name: string; }

type ValidRateTier =
    | 'FrozenStorage'
    | 'Chilling'
    | 'CoolStorage'
    | 'DeepFrozen'
    | 'ULT'
    | 'Kg'
    | 'Each'
    | 'Hour'
    | 'Shipment'
    | 'Expedited'
    | '';

@Component({
  selector: 'app-rate-detail',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDatepickerModule
  ],
  templateUrl: './rate-detail.component.html',
  styleUrls: ['./rate-detail.component.scss']
})
export class RateDetailComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminApi = inject(AdminApiService);
  private http = inject(HttpClient);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  rateForm: FormGroup;
  rateId = signal<string | null>(null);
  isEditMode = signal(false);
  isLoading = signal(true);
  isSaving = signal(false);
  isDeleting = signal(false);

  accounts = signal<AccountDto[]>([]);

  // --- ADD THIS CONSTANT ---
  readonly EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

  serviceTypes: ServiceType[] = [
    'Storage', 'Blasting', 'Handling', 'VAS',
    'FrozenStorage', 'Chilling', 'CoolStorage', 'DeepFrozenStorage', 'ULTStorage',
    'Repack', 'Split', 'Labeling', 'CrossDock', 'Fumigation', 'Surcharge',
    'CycleCount', 'Kitting'
  ];

  validRateTiers: ValidRateTier[] = [
    '', // Represents the default/global, untiered rate
    'FrozenStorage',
    'Chilling',
    'CoolStorage',
    'DeepFrozen',
    'ULT',
    'Kg',
    'Each',
    'Hour',
    'Shipment',
    'Expedited'
  ];

  uoms: RateUom[] = ['Pallet', 'Kg', 'Day', 'Cycle', 'Each', 'Hour', 'Shipment', 'Percent'];

  constructor() {
    this.rateForm = this.fb.group({
      accountId: [null], // Form uses 'null' to represent the "Default" option
      serviceType: ['', Validators.required],
      uom: ['', Validators.required],
      tier: [''],
      value: [null, [Validators.required, Validators.min(0)]],
      effectiveStartDate: [new Date(), Validators.required],
      effectiveEndDate: [null]
    });
  }

  ngOnInit(): void {
    this.loadLookups();

    this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (id && id !== 'new') {
          this.rateId.set(id);
          this.isEditMode.set(false);
          this.rateForm.disable();
          return this.adminApi.getRateById(id);
        } else {
          this.rateId.set(null);
          this.isEditMode.set(true);
          this.rateForm.enable();
          this.isLoading.set(false);
          return Promise.resolve(null);
        }
      })
    ).subscribe(rate => {
      if (rate) {
        // Convert EMPTY_GUID from DB to 'null' for the form ---
        const formValue = { ...rate, accountId: rate.accountId === this.EMPTY_GUID ? null : rate.accountId };
        this.rateForm.patchValue(formValue);
      }
      this.isLoading.set(false);
    });
  }

  loadLookups(): void {
    this.http.get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`).subscribe(data => this.accounts.set(data));
  }

  toggleEditMode(isEditing: boolean): void {
    this.isEditMode.set(isEditing);
    isEditing ? this.rateForm.enable() : this.rateForm.disable();
  }

  save(): void {
    if (this.rateForm.invalid) return;
    this.isSaving.set(true);

    // Convert 'null' from form back to EMPTY_GUID for the backend ---
    const formValue = this.rateForm.value;
    const accountIdToSend = formValue.accountId === null ? this.EMPTY_GUID : formValue.accountId;

    if (this.rateId()) {
      const command: UpdateRateCommand = { id: this.rateId()!, ...formValue, accountId: accountIdToSend };
      this.adminApi.updateRate(this.rateId()!, command).subscribe({
        next: () => this.handleSaveSuccess('Rate updated successfully (New Revision Created).'),
        error: (err: any) => this.handleSaveError(err)
      });
    } else {
      const command: CreateRateCommand = { ...formValue, accountId: accountIdToSend };
      this.adminApi.createRate(command).subscribe({
        next: (newId) => {
          this.handleSaveSuccess('Rate created successfully.');
          this.router.navigate(['/admin/rates/detail', newId], { replaceUrl: true });
        },
        error: (err: any) => this.handleSaveError(err)
      });
    }
  }

  private handleSaveSuccess(message: string): void {
    this.isSaving.set(false);
    this.isEditMode.set(false);
    this.rateForm.disable();
    this.snackBar.open(message, 'OK', { duration: 3000 });
    this.router.navigate(['/admin/rates']);
  }

  private handleSaveError(err: any): void {
    this.isSaving.set(false);
    this.snackBar.open(`Error: ${err.error?.title || 'Failed to save rate.'}`, 'Close');
  }

  delete(): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Rate',
        message: `Are you sure you want to delete this rate? This action cannot be undone.`
      }
    });

    dialogRef.afterClosed().pipe(filter(result => result === true)).subscribe(() => {
      this.isDeleting.set(true);
      this.adminApi.deleteRate(this.rateId()!).subscribe({
        next: () => {
          this.snackBar.open('Rate deleted successfully.', 'OK', { duration: 3000 });
          this.router.navigate(['/admin/rates']);
        },
        error: (err: any) => {
          this.snackBar.open(`Error: ${err.error?.title || 'Failed to delete rate.'}`, 'Close');
          this.isDeleting.set(false);
        }
      });
    });
  }

  back(): void
  {
    this.router.navigate(['/admin/rates']);
  }
}
