import {
  Component,
  OnInit,
  AfterViewInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  FormControl,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule, Sort } from '@angular/material/sort';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  Subject,
  merge,
  switchMap,
  map,
  catchError,
  of,
  filter,
  debounceTime,
  EMPTY,
  startWith,
} from 'rxjs';
import {
  AdminSetupApiService,
  CreateLocationsInBayCommand,
  CreateRoomCommand,
  CreateSimpleLocationCommand,
  LocationDto,
  RoomDetailDto,
  ServiceType,
  WarehouseDto,
  GetLocationsForRoomQuery,
} from '../../admin-setup-api.service';
import { ConfirmationDialogComponent } from '../../../../shared/confirmation-dialog/confirmation-dialog.component';
import { EditLocationDialogComponent } from '../edit-location-dialog/edit-location-dialog.component';

@Component({
  selector: 'app-room-detail',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    MatExpansionModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatTooltipModule,
    DecimalPipe,
  ],
  templateUrl: './room-detail.component.html',
  styleUrls: ['./room-detail.component.scss'],
})
export class RoomDetailComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminSetupApi = inject(AdminSetupApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  roomId = signal<string | null>(null);
  isNewRoom = signal(false);
  isLoading = signal(true);
  isSaving = signal(false);
  isDeleting = signal(false);
  isGeneratingLocations = signal(false);
  isAddingSimpleLocation = signal(false);
  isEditMode = signal(false);
  resultsLength = signal(0);
  isLoadingTable = signal(true);

  roomDetails = signal<RoomDetailDto | null>(null);
  warehouses = signal<WarehouseDto[]>([]);
  serviceTypes: ServiceType[] = [
    'Storage',
    'Blasting',
    'Chilling',
    'CoolStorage',
    'DeepFrozenStorage',
    'FrozenStorage',
    'ULTStorage',
    'Handling',
    'Staging',
    'CrossDock',
  ];
  locationTypes: ('Staging' | 'Picking')[] = ['Staging', 'Picking'];

  roomForm: FormGroup;
  generateRackForm: FormGroup;
  simpleLocationForm: FormGroup;
  locationSearchControl = new FormControl('');

  private _paginator!: MatPaginator;
  @ViewChild(MatPaginator)
  set paginator(paginator: MatPaginator) {
    if (paginator) {
      this._paginator = paginator;
      this.trySetupTableListeners();
    }
  }
  get paginator(): MatPaginator {
    return this._paginator;
  }

  private _sort!: MatSort;
  @ViewChild(MatSort)
  set sort(sort: MatSort) {
    if (sort) {
      this._sort = sort;
      this.trySetupTableListeners();
    }
  }
  get sort(): MatSort {
    return this._sort;
  }

  displayedColumns = [
    'barcode',
    'bay',
    'row',
    'column',
    'level',
    'zoneType',
    'capacityWeight',
    'isEmpty',
    'isActive',
    'actions',
  ];
  dataSource = new MatTableDataSource<LocationDto>();
  private refreshTable = new Subject<void>();
  private tableListenersInitialized = false;

  constructor() {
    this.roomForm = this.fb.group({
      warehouseId: ['', Validators.required],
      name: ['', Validators.required],
      minTemp: [-20, Validators.required],
      maxTemp: [-10, Validators.required],
      serviceType: ['FrozenStorage' as ServiceType, Validators.required],
    });

    this.generateRackForm = this.fb.group({
      bay: ['A01', Validators.required],
      startRow: [1, [Validators.required, Validators.min(1)]],
      endRow: [10, [Validators.required, Validators.min(1)]],
      startCol: [1, [Validators.required, Validators.min(1)]],
      endCol: [5, [Validators.required, Validators.min(1)]],
      startLevel: [1, [Validators.required, Validators.min(1)]],
      endLevel: [6, [Validators.required, Validators.min(1)]],
      zoneType: ['Storage' as const],
    });

    this.simpleLocationForm = this.fb.group({
      name: ['STAGING-01', Validators.required],
      zoneType: ['Staging' as 'Staging' | 'Picking', Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadWarehouses();

    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (id && id !== 'new') {
            this.isNewRoom.set(false);
            this.roomId.set(id);
            this.roomForm.get('warehouseId')?.disable();
            return this.adminSetupApi.getRoomById(id).pipe(
              catchError(() => {
                this.snackBar.open('Room not found.', 'Close', {
                  duration: 3000,
                });
                this.router.navigate(['/setup/rooms']);
                return EMPTY;
              })
            );
          } else {
            this.isNewRoom.set(true);
            this.isLoading.set(false);
            this.roomForm.enable();
            this.isEditMode.set(true);
            return of(null);
          }
        })
      )
      .subscribe((room) => {
        if (room) {
          this.roomDetails.set(room);
          this.roomForm.patchValue({
            warehouseId: room.warehouseId,
            name: room.name,
            minTemp: room.minTemp,
            maxTemp: room.maxTemp,
            serviceType: room.serviceType,
          });
          this.isLoading.set(false);
          this.roomForm.disable();
          this.isEditMode.set(false);
        }
      });
  }

  trySetupTableListeners(): void {
    if (
      this.isNewRoom() ||
      !this.paginator ||
      !this.sort ||
      this.tableListenersInitialized
    ) {
      return;
    }

    this.tableListenersInitialized = true;

    setTimeout(() => {
      this.sort.active = 'bay';
      this.sort.direction = 'asc';
    }, 0);

    this.sort.active = 'bay';
    this.sort.direction = 'asc';

    merge(
      this.sort.sortChange,
      this.paginator.page,
      this.refreshTable,
      this.locationSearchControl.valueChanges.pipe(debounceTime(400))
    )
      .pipe(
        startWith({}),
        switchMap(() => {
          this.isLoadingTable.set(true);
          const query: GetLocationsForRoomQuery = {
            roomId: this.roomId()!,
            page: this.paginator.pageIndex + 1,
            pageSize: this.paginator.pageSize,
            sortBy: this.sort.active,
            sortDirection: this.sort.direction
              ? this.sort.direction
              : undefined,
            searchTerm: this.locationSearchControl.value ?? undefined,
          };
          return this.adminSetupApi.getLocationsForRoom(query).pipe(
            catchError(() => {
              this.snackBar.open('Failed to load locations.', 'Close');
              return of(null);
            })
          );
        }),
        map((data) => {
          this.isLoadingTable.set(false);
          if (data) {
            this.resultsLength.set(data.totalCount);
            return data.items;
          }
          return [];
        })
      )
      .subscribe((items) => (this.dataSource.data = items));
  }

  loadWarehouses(): void {
    this.adminSetupApi
      .getWarehouses()
      .subscribe((data) => this.warehouses.set(data));
  }

  saveRoom(): void {
    if (this.roomForm.invalid) {
      this.snackBar.open(
        'Please fill in all required fields for the room.',
        'Close'
      );
      return;
    }
    this.isSaving.set(true);
    const formValue = this.roomForm.getRawValue();

    if (this.isNewRoom()) {
      const command: CreateRoomCommand = {
        warehouseId: formValue.warehouseId,
        name: formValue.name,
        minTemp: formValue.minTemp,
        maxTemp: formValue.maxTemp,
        serviceType: formValue.serviceType,
      };
      this.adminSetupApi.createRoom(command).subscribe({
        next: (newId) => {
          this.snackBar.open('Room created successfully.', 'OK', {
            duration: 3000,
          });
          this.router.navigate(['/setup/rooms/detail', newId], {
            replaceUrl: true,
          });
        },
        error: (err) => this.handleSaveError(err),
      });
    } else {
      this.router.navigate(['/setup/rooms']);
      this.snackBar.open(
        'Room update functionality not yet implemented.',
        'OK'
      );
      this.isSaving.set(false);
    }
  }

  handleSaveError(err: any): void {
    this.isSaving.set(false);
    this.snackBar.open(
      `Error: ${err.error?.title || 'Failed to save room.'}`,
      'Close',
      { duration: 5000 }
    );
  }

  onGenerateRacks(): void {
    if (this.generateRackForm.invalid) return;
    this.isGeneratingLocations.set(true);

    const cmd: CreateLocationsInBayCommand = {
      roomId: this.roomId()!,
      ...this.generateRackForm.value,
    };

    this.adminSetupApi.createLocationsInBay(cmd).subscribe({
      next: () => {
        this.snackBar.open('Rack locations generated successfully!', 'OK', {
          duration: 3000,
        });
        const currentBay = this.generateRackForm.get('bay')?.value || 'A01';
        const nextBay = currentBay.replace(/(\d+)$/, (match: string) =>
          (parseInt(match) + 1).toString().padStart(match.length, '0')
        );
        this.generateRackForm.patchValue({ bay: nextBay });

        this.refreshTable.next();
        this.isGeneratingLocations.set(false);
      },
      error: (err) => {
        this.snackBar.open(
          `Error: ${err.error?.title || 'Failed to generate locations.'}`,
          'Close'
        );
        this.isGeneratingLocations.set(false);
      },
    });
  }

  onAddSimpleLocation(): void {
    if (this.simpleLocationForm.invalid) return;
    this.isAddingSimpleLocation.set(true);

    const cmd: CreateSimpleLocationCommand = {
      roomId: this.roomId()!,
      ...this.simpleLocationForm.value,
    };

    this.adminSetupApi.createSimpleLocation(cmd).subscribe({
      next: () => {
        this.snackBar.open('Simple location created!', 'OK', {
          duration: 3000,
        });
        const currentName =
          this.simpleLocationForm.get('name')?.value || 'STAGING-01';
        const nextName = currentName.replace(/(\d+)$/, (match: string) =>
          (parseInt(match) + 1).toString().padStart(match.length, '0')
        );
        this.simpleLocationForm.patchValue({ name: nextName });

        this.refreshTable.next();
        this.isAddingSimpleLocation.set(false);
      },
      error: (err) => {
        this.snackBar.open(
          `Error: ${err.error?.title || 'Failed to create location.'}`,
          'Close'
        );
        this.isAddingSimpleLocation.set(false);
      },
    });
  }

  openEditLocationDialog(location: LocationDto): void {
    const dialogRef = this.dialog.open(EditLocationDialogComponent, {
      width: '550px',
      data: { location },
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result) => result === true))
      .subscribe(() => {
        this.refreshTable.next();
      });
  }

  deleteLocation(location: LocationDto): void {
    if (!location.isEmpty) {
      this.snackBar.open(
        'Cannot delete a location that is not empty.',
        'Close',
        { duration: 4000 }
      );
      return;
    }

    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Location',
        message: `Are you sure you want to delete location "${location.barcode}"? This action cannot be undone.`,
      },
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result) => result === true))
      .subscribe(() => {
        this.adminSetupApi.deleteLocation(location.id).subscribe({
          next: () => {
            this.snackBar.open('Location deleted successfully.', 'OK', {
              duration: 3000,
            });
            this.refreshTable.next();
          },
          error: (err) =>
            this.snackBar.open(
              `Error: ${err.error?.title || 'Failed to delete location.'}`,
              'Close'
            ),
        });
      });
  }

  back(): void {
    this.router.navigate(['/setup/rooms']);
  }

  get addressFormGroup(): FormGroup {
    return this.roomForm.get('address') as FormGroup;
  }
}
