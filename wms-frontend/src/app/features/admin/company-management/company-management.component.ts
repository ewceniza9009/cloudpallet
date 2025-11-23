import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import {
  AdminApiService,
  CompanyDto,
  UpdateCompanyCommand,
} from '../admin-api.service';

@Component({
  selector: 'app-company-management',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatSelectModule,
  ],
  templateUrl: './company-management.component.html',
  styleUrls: ['./company-management.component.scss'],
})
export class CompanyManagementComponent implements OnInit {
  private fb = inject(FormBuilder);
  private adminApi = inject(AdminApiService);
  private snackBar = inject(MatSnackBar);

  companyForm: FormGroup;
  isLoading = signal(true);
  isSaving = signal(false);
  isEditMode = signal(false);
  companyDetails = signal<CompanyDto | null>(null);

  constructor() {
    this.companyForm = this.fb.group({
      name: ['', Validators.required],
      taxId: ['', Validators.required],
      address: this.fb.group({
        street: ['', Validators.required],
        city: ['', Validators.required],
        state: ['', Validators.required],
        postalCode: ['', Validators.required],
        country: ['', Validators.required],
      }),
      phoneNumber: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      website: [''],
      gs1CompanyPrefix: ['', [Validators.maxLength(20)]],
      defaultBarcodeFormat: ['SSCC-18', [Validators.maxLength(20)]],
    });

    this.companyForm.disable();
  }

  ngOnInit(): void {
    this.loadCompanyDetails();
  }

  loadCompanyDetails(): void {
    this.isLoading.set(true);
    this.adminApi.getCompanyDetails().subscribe({
      next: (data) => {
        this.companyDetails.set(data);
        this.companyForm.patchValue(data);
        this.isLoading.set(false);
        this.isEditMode.set(false);
        this.companyForm.disable();
      },
      error: () => {
        this.snackBar.open('Failed to load company details.', 'Close', {
          duration: 5000,
        });
        this.isLoading.set(false);
      },
    });
  }

  toggleEditMode(edit: boolean): void {
    this.isEditMode.set(edit);
    if (edit) {
      this.companyForm.enable();
    } else {
      if (this.companyDetails()) {
        this.companyForm.patchValue(this.companyDetails()!);
      }
      this.companyForm.disable();
    }
  }

  save(): void {
    if (this.companyForm.invalid) {
      this.snackBar.open('Please fill in all required fields.', 'Close', {
        duration: 3000,
      });
      this.companyForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const command: UpdateCompanyCommand = this.companyForm.value;

    this.adminApi.updateCompanyDetails(command).subscribe({
      next: () => {
        this.snackBar.open('Company details updated successfully.', 'OK', {
          duration: 3000,
        });
        this.isSaving.set(false);
        this.toggleEditMode(false);

        this.loadCompanyDetails();
      },
      error: (err) => {
        this.snackBar.open(
          `Error: ${err.error?.title || 'Failed to update company details.'}`,
          'Close',
          { duration: 5000 }
        );
        this.isSaving.set(false);
      },
    });
  }

  get addressFormGroup(): FormGroup {
    return this.companyForm.get('address') as FormGroup;
  }
}
