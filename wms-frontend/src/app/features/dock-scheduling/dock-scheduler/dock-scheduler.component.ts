import { CommonModule, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { of, Subject, Subscription } from 'rxjs';
import {
  debounceTime,
  distinctUntilChanged,
  filter,
  map,
  startWith,
  switchMap,
  takeUntil,
  tap,
} from 'rxjs/operators';

import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { DockAppointmentDto } from '../../../core/models/dock-appointment.dto';
import { DockApiService, DockDto, ScheduleAppointmentCommand } from '../dock-api.service';
import { DockCalendarComponent } from '../dock-calendar/dock-calendar.component';
import { WarehouseStateService } from '../../../core/services/warehouse-state.service';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AppointmentDetailsDialogComponent } from '../appointment-details-dialog/appointment-details-dialog.component';

type ValidSelectionForm = { dockId: string; selectedDate: Date };
interface SupplierDto {
  id: string;
  name: string;
}
interface AccountDto {
  id: string;
  name: string;
}
interface TruckDto {
  id: string;
  licensePlate: string;
}

@Component({
  selector: 'app-dock-scheduler',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    MatSelectModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCardModule,
    MatListModule,
    MatButtonModule,
    MatSnackBarModule,
    MatRadioModule,
    MatAutocompleteModule,
    DockCalendarComponent,
    MatButtonToggleModule,
    MatDialogModule
  ],
  templateUrl: './dock-scheduler.component.html',
  styleUrls: ['./dock-scheduler.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DockSchedulerComponent implements OnInit, OnDestroy {
  private dockService = inject(DockApiService);
  private fb = inject(FormBuilder);
  private snackBar = inject(MatSnackBar);
  private http = inject(HttpClient);
  private warehouseState = inject(WarehouseStateService);
  private dialog = inject(MatDialog);
  private destroy$ = new Subject<void>();

  docks = signal<DockDto[]>([]);
  appointments = signal<DockAppointmentDto[]>([]);
  suppliers = signal<SupplierDto[]>([]);
  accounts = signal<AccountDto[]>([]);
  trucks = signal<TruckDto[]>([]);
  isLoading = signal(true);
  isSubmitting = signal(false);
  error = signal<string | null>(null);
  filteredSuppliers = signal<SupplierDto[]>([]);
  filteredAccounts = signal<AccountDto[]>([]);
  filteredTrucks = signal<TruckDto[]>([]);
  
  viewMode = signal<'list' | 'calendar'>('list');
  calendarAppointments = signal<DockAppointmentDto[]>([]);

  searchControl = new FormControl('');
  selectionForm = this.fb.group({
    dockId: ['', Validators.required],
    selectedDate: [new Date(), Validators.required],
  });

  supplierControl = new FormControl<SupplierDto | string | null>(null, Validators.required);
  accountControl = new FormControl<AccountDto | string | null>(null, Validators.required);
  truckControl = new FormControl<TruckDto | string | null>(null, Validators.required);
  appointmentForm = this.fb.group({
    licensePlate: ['', [Validators.required, Validators.minLength(3)]],
    supplierId: ['', Validators.required],
    accountId: ['', Validators.required],
    startTime: ['09:00', Validators.required],
    endTime: ['10:00', Validators.required],
    type: ['Receiving', Validators.required],
  });

  private autocompleteSubs: Subscription[] = [];

  public truckDisplayFn = (item: TruckDto): string => {
    return this.getDisplayName(item, 'licensePlate');
  };

  constructor() {
    effect(() => {
      const isSearching = !!this.searchControl.value;
      const dockIdControl = this.selectionForm.controls.dockId;
      const dateControl = this.selectionForm.controls.selectedDate;
      if (isSearching) {
        dockIdControl.disable({ emitEvent: false });
        dateControl.disable({ emitEvent: false });
      } else {
        dockIdControl.enable({ emitEvent: false });
        dateControl.enable({ emitEvent: false });
      }
    });
  }

  ngOnInit(): void {
    this.loadDocks();
    this.loadLookups();
    this.setupSearchAndSelectionListener();
    this.setupAutocompletes();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.autocompleteSubs.forEach(sub => sub.unsubscribe());
  }

  loadLookups(): void {
    this.http
      .get<SupplierDto[]>(`${environment.apiUrl}/Lookups/suppliers`)
      .subscribe(data => {
        this.suppliers.set(data);
        this.supplierControl.updateValueAndValidity({ emitEvent: true });
      });
    this.http
      .get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe(data => {
        this.accounts.set(data);
        this.accountControl.updateValueAndValidity({ emitEvent: true });
      });
    this.http
      .get<TruckDto[]>(`${environment.apiUrl}/Lookups/trucks`)
      .subscribe(data => {
        this.trucks.set(data);
        this.truckControl.updateValueAndValidity({ emitEvent: true });
      });
  }

  loadDocks(): void {
    this.isLoading.set(true);
    this.dockService.getDocks().subscribe({
      next: docks => {
        this.docks.set(docks);
        if (docks.length > 0) {
          this.selectionForm.controls.dockId.setValue(docks[0].id);
        } else {
          this.error.set('No docks available.');
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load dock information.');
        this.isLoading.set(false);
      },
    });
  }

  setupAutocompletes(): void {
    const filterFn = (
      control: FormControl<any>,
      optionsSignal: any,
      filteredSignal: any,
      displayProp: 'name' | 'licensePlate' = 'name'
    ) => {
      const sub = control.valueChanges
        .pipe(
          startWith(''),
          map(value =>
            typeof value === 'string' ? value : this.getDisplayName(value, displayProp)
          ),
          map(name =>
            name
              ? this._filter(name, optionsSignal() as any, displayProp)
              : (optionsSignal().slice() as any[])
          )
        )
        .subscribe(data => filteredSignal.set(data as any[])); // Cast to any[]
      this.autocompleteSubs.push(sub);
    };

    filterFn(this.supplierControl, this.suppliers, this.filteredSuppliers);
    filterFn(this.accountControl, this.accounts, this.filteredAccounts);
    filterFn(this.truckControl, this.trucks, this.filteredTrucks, 'licensePlate');
  }

  private _filter(
    value: string,
    options: any[],
    filterProp: 'name' | 'licensePlate' = 'name'
  ): any[] {
    const filterValue = value?.toLowerCase() || '';
    return options.filter(option => option[filterProp].toLowerCase().includes(filterValue));
  }

  getDisplayName(item: any, displayProp: 'name' | 'licensePlate' = 'name'): string {
    if (!item) return '';
    return item[displayProp] ?? '';
  }

  onSupplierSelected(event: MatAutocompleteSelectedEvent): void {
    const selectedSupplier: SupplierDto = event.option.value;
    this.appointmentForm.controls.supplierId.setValue(selectedSupplier.id);
  }

  onAccountSelected(event: MatAutocompleteSelectedEvent): void {
    const selectedAccount: AccountDto = event.option.value;
    this.appointmentForm.controls.accountId.setValue(selectedAccount.id);
  }

  onTruckSelected(event: MatAutocompleteSelectedEvent): void {
    const selectedTruck: TruckDto = event.option.value;
    this.appointmentForm.controls.licensePlate.setValue(selectedTruck.licensePlate);
  }

  setupSearchAndSelectionListener(): void {
    this.selectionForm.valueChanges
      .pipe(
        filter(
          (value): value is ValidSelectionForm =>
            !!value.dockId && !!value.selectedDate && !this.searchControl.value
        ),
        tap(() => this.isLoading.set(true)),
        switchMap(formValue => {
          const selectedDate = formValue.selectedDate as Date;
          const startOfDay = new Date(
            Date.UTC(
              selectedDate.getFullYear(),
              selectedDate.getMonth(),
              selectedDate.getDate(),
              0,
              0,
              0,
              0
            )
          );
          const endOfDay = new Date(startOfDay.getTime());
          endOfDay.setUTCDate(endOfDay.getUTCDate() + 1);

          return this.dockService.getAppointmentsForDock(
            formValue.dockId,
            startOfDay.toISOString(),
            endOfDay.toISOString()
          );
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(data => {
        this.appointments.set(data);
        this.isLoading.set(false);
      });

    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => this.isLoading.set(true)),
        switchMap(searchTerm => {
          if (!searchTerm || searchTerm.length < 2) {
            this.selectionForm.updateValueAndValidity({ emitEvent: true });
            return of([]);
          }

          return this.dockService.searchAppointmentsByPlate(searchTerm);
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(results => {
        this.appointments.set(results);
        this.isLoading.set(false);
      });
  }

  onSubmit(): void {
    if (this.supplierControl.value && typeof this.supplierControl.value === 'object') {
      this.appointmentForm.controls.supplierId.setValue(this.supplierControl.value.id);
    } else if (typeof this.supplierControl.value !== 'string') {
      this.appointmentForm.controls.supplierId.setValue('');
    }

    if (this.accountControl.value && typeof this.accountControl.value === 'object') {
      this.appointmentForm.controls.accountId.setValue(this.accountControl.value.id);
    } else if (typeof this.accountControl.value !== 'string') {
      this.appointmentForm.controls.accountId.setValue('');
    }

    if (this.truckControl.value && typeof this.truckControl.value === 'object') {
      this.appointmentForm.controls.licensePlate.setValue(this.truckControl.value.licensePlate);
    } else if (typeof this.truckControl.value !== 'string') {
      this.appointmentForm.controls.licensePlate.setValue('');
    }

    if (this.appointmentForm.invalid || !this.selectionForm.valid) {
      this.snackBar.open('Please fill all required fields correctly.', 'Close', { duration: 4000 });
      return;
    }
    this.isSubmitting.set(true);

    const selection = this.selectionForm.getRawValue();
    const formValue = this.appointmentForm.getRawValue();
    const selectedDate = selection.selectedDate!;

    const [startHour, startMinute] = formValue.startTime!.split(':').map(Number);
    const startDateTime = new Date(
      selectedDate.getFullYear(),
      selectedDate.getMonth(),
      selectedDate.getDate(),
      startHour,
      startMinute,
      0,
      0
    );

    const [endHour, endMinute] = formValue.endTime!.split(':').map(Number);
    let endDateTime = new Date(
      selectedDate.getFullYear(),
      selectedDate.getMonth(),
      selectedDate.getDate(),
      endHour,
      endMinute,
      0,
      0
    );

    if (endDateTime <= startDateTime) {
      endDateTime.setDate(endDateTime.getDate() + 1);
    }

    if (startDateTime >= endDateTime) {
      this.snackBar.open('Error: Calculated Start time must be before End time.', 'Close', {
        duration: 5000,
      });
      this.isSubmitting.set(false);
      return;
    }

    const durationHours = (endDateTime.getTime() - startDateTime.getTime()) / (1000 * 60 * 60);
    if (durationHours <= 0 || durationHours > 24) {
      this.snackBar.open(
        `Error: Appointment duration is invalid or exceeds 24 hours (${durationHours.toFixed(
          1
        )}h).`,
        'Close',
        { duration: 6000 }
      );
      this.isSubmitting.set(false);
      return;
    }

    const command: ScheduleAppointmentCommand = {
      dockId: selection.dockId!,
      licensePlate: formValue.licensePlate!,
      supplierId: formValue.supplierId!,
      accountId: formValue.accountId!,
      startDateTime: startDateTime.toISOString(),
      endDateTime: endDateTime.toISOString(),
      type: formValue.type as 'Receiving' | 'Shipping',
    };

    this.dockService.scheduleAppointment(command).subscribe({
      next: () => {
        this.snackBar.open(`Appointment scheduled successfully!`, 'OK', {
          duration: 3000,
        });

        this.appointmentForm.reset({
          licensePlate: '',
          supplierId: '',
          accountId: '',
          startTime: '09:00',
          endTime: '10:00',
          type: 'Receiving',
        });

        this.supplierControl.reset();
        this.accountControl.reset();
        this.truckControl.reset();

        this.selectionForm.updateValueAndValidity({ emitEvent: true });

        this.isSubmitting.set(false);
      },
      error: err => {
        const message = err.error?.title || 'The slot may be taken or input is invalid.';
        this.snackBar.open(`Failed to schedule: ${message}`, 'Close', {
          duration: 7000,
        });
        this.isSubmitting.set(false);
      },
    });
  }


  currentViewStart = signal<Date>(new Date());
  currentViewEnd = signal<Date>(new Date());

  onViewModeChange(mode: 'list' | 'calendar') {
    this.viewMode.set(mode);
    if (mode === 'calendar') {
      // Initial load will be triggered by the calendar component emitting date range
    }
  }

  onCalendarDateRangeChanged(range: { start: Date, end: Date }) {
    this.currentViewStart.set(range.start);
    this.currentViewEnd.set(range.end);
    this.loadCalendarEvents(range.start, range.end);
  }

  loadCalendarEvents(start: Date, end: Date) {
    const warehouseId = this.warehouseState.selectedWarehouseId();
    if (!warehouseId) return;

    this.isLoading.set(true);
    this.dockService.getAppointmentsForWarehouse(warehouseId, start.toISOString(), end.toISOString())
      .subscribe({
        next: (data) => {
          this.calendarAppointments.set(data);
          this.isLoading.set(false);
        },
        error: () => {
          this.snackBar.open('Failed to load calendar events', 'Close', { duration: 3000 });
          this.isLoading.set(false);
        }
      });
  }

  openAppointmentDetails(appointment: DockAppointmentDto) {
    const dialogRef = this.dialog.open(AppointmentDetailsDialogComponent, {
      data: appointment,
      width: '400px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadCalendarEvents(this.currentViewStart(), this.currentViewEnd());
      }
    });
  }

  onAppointmentRescheduled(event: { appointment: DockAppointmentDto, newStart: Date, newEnd: Date }) {
    this.isLoading.set(true);
    this.dockService.rescheduleAppointment(event.appointment.id, event.newStart, event.newEnd)
      .subscribe({
        next: () => {
          this.snackBar.open('Appointment rescheduled successfully', 'OK', { duration: 3000 });
          // Reload events using the current view range to keep all events visible
          this.loadCalendarEvents(this.currentViewStart(), this.currentViewEnd());
        },
        error: (err) => {
          this.snackBar.open('Failed to reschedule appointment', 'Close', { duration: 5000 });
          this.isLoading.set(false);
          // Reload current view range to revert any optimistic UI changes if necessary
          this.loadCalendarEvents(this.currentViewStart(), this.currentViewEnd());
        }
      });
  }
}
