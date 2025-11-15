import {
  Component,
  OnInit,
  inject,
  signal,
  ViewChild,
  AfterViewInit,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { filter, switchMap, EMPTY, catchError, of, merge } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../../shared/confirmation-dialog/confirmation-dialog.component';
import {
  AdminSetupApiService,
  CarrierDetailDto,
  CreateCarrierCommand,
  UpdateCarrierCommand,
  TruckDto,
} from '../../admin-setup-api.service';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EditTruckDialogComponent } from '../edit-truck-dialog/edit-truck-dialog.component';

@Component({
  selector: 'app-carrier-detail',
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
    MatSlideToggleModule,
    MatDialogModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatDatepickerModule,
    MatCheckboxModule,
    MatTooltipModule,
  ],
  templateUrl: './carrier-detail.component.html',
  styleUrls: ['./carrier-detail.component.scss'],
})
export class CarrierDetailComponent implements OnInit, AfterViewInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  carrierForm: FormGroup;
  carrierId = signal<string | null>(null);
  isEditMode = signal(false);
  isLoading = signal(true);
  isSaving = signal(false);
  isDeleting = signal(false);

  isLoadingTrucks = signal(true);
  truckDataSource = new MatTableDataSource<TruckDto>();
  displayedTruckColumns = [
    'licensePlate',
    'model',
    'capacityWeight',
    'isActive',
    'actions',
  ];

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor() {
    this.carrierForm = this.fb.group({
      name: ['', Validators.required],
      scacCode: ['', Validators.required],
      address: this.fb.group({
        street: ['', Validators.required],
        city: ['', Validators.required],
        state: ['', Validators.required],
        postalCode: ['', Validators.required],
        country: ['', Validators.required],
      }),
      contactName: [''],
      contactPhone: [''],
      contactEmail: ['', [Validators.email]],
      certificationColdChain: [false],
      insurancePolicyNumber: [''],
      insuranceExpiryDate: [null as Date | null],
      isActive: [true],
    });
  }

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (id && id !== 'new') {
            this.carrierId.set(id);
            this.isEditMode.set(false);
            this.carrierForm.disable();
            return this.adminSetupApi.getCarrierById(id).pipe(
              catchError(() => {
                this.snackBar.open('Carrier not found.', 'Close', {
                  duration: 3000,
                });
                this.router.navigate(['/setup/carriers']);
                return EMPTY;
              })
            );
          } else {
            this.carrierId.set(null);
            this.isEditMode.set(true);
            this.carrierForm.enable();
            this.isLoading.set(false);
            this.isLoadingTrucks.set(false);
            return of(null);
          }
        })
      )
      .subscribe((carrier) => {
        if (carrier) {
          this.carrierForm.patchValue(carrier);
          this.loadTrucks();
        }
        this.isLoading.set(false);
      });
  }

  ngAfterViewInit(): void {
    this.truckDataSource.paginator = this.paginator;
    this.truckDataSource.sort = this.sort;
  }

  loadTrucks(): void {
    const id = this.carrierId();
    if (!id) return;

    this.isLoadingTrucks.set(true);
    this.adminSetupApi.getTrucksByCarrier(id).subscribe({
      next: (trucks) => {
        this.truckDataSource.data = trucks;
        this.isLoadingTrucks.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load trucks for this carrier.', 'Close');
        this.isLoadingTrucks.set(false);
      },
    });
  }

  toggleEditMode(isEditing: boolean): void {
    this.isEditMode.set(isEditing);
    if (isEditing) {
      this.carrierForm.enable();
    } else {
      this.carrierForm.disable();

      this.adminSetupApi
        .getCarrierById(this.carrierId()!)
        .subscribe((data) => this.carrierForm.patchValue(data));
    }
  }

  save(): void {
    if (this.carrierForm.invalid) {
      this.snackBar.open('Please fill in all required fields.', 'Close', {
        duration: 3000,
      });
      this.carrierForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.carrierForm.getRawValue();

    if (this.carrierId()) {
      const command: UpdateCarrierCommand = {
        id: this.carrierId()!,
        ...formValue,
      };
      this.adminSetupApi.updateCarrier(this.carrierId()!, command).subscribe({
        next: () => this.handleSaveSuccess('Carrier updated successfully.'),
        error: (err: any) => this.handleSaveError(err),
      });
    } else {
      const command: CreateCarrierCommand = formValue;
      this.adminSetupApi.createCarrier(command).subscribe({
        next: (newId) => {
          this.handleSaveSuccess('Carrier created successfully.');
          this.router.navigate(['/setup/carriers/detail', newId], {
            replaceUrl: true,
          });
        },
        error: (err: any) => this.handleSaveError(err),
      });
    }
  }

  private handleSaveSuccess(message: string): void {
    this.isSaving.set(false);
    this.isEditMode.set(false);
    this.carrierForm.disable();
    this.snackBar.open(message, 'OK', { duration: 3000 });
    this.router.navigate(['/setup/carriers']);
  }

  private handleSaveError(err: any): void {
    this.isSaving.set(false);
    this.snackBar.open(
      `Error: ${err.error?.title || 'Failed to save carrier.'}`,
      'Close'
    );
  }

  delete(): void {}

  back(): void {
    this.router.navigate(['/setup/carriers']);
  }

  get addressFormGroup(): FormGroup {
    return this.carrierForm.get('address') as FormGroup;
  }

  openTruckDialog(truck?: TruckDto): void {
    const dialogRef = this.dialog.open(EditTruckDialogComponent, {
      width: '500px',
      data: {
        carrierId: this.carrierId(),
        truck: truck,
      },
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result) => result === true))
      .subscribe(() => {
        this.loadTrucks();
      });
  }

  deleteTruck(truck: TruckDto): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Truck',
        message: `Are you sure you want to delete truck "${truck.licensePlate}"?`,
      },
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result) => result === true))
      .subscribe(() => {
        this.adminSetupApi.deleteTruck(truck.id).subscribe({
          next: () => {
            this.snackBar.open('Truck deleted.', 'OK', { duration: 2000 });
            this.loadTrucks();
          },
          error: (err) =>
            this.snackBar.open(
              `Error: ${err.error?.title || 'Failed to delete truck.'}`,
              'Close'
            ),
        });
      });
  }
}
