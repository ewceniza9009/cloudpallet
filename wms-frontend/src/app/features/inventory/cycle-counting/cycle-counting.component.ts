import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormArray,
  FormControl,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import {
  debounceTime,
  distinctUntilChanged,
  filter,
  map,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import {
  InventoryApiService,
  RepackableInventoryDto,
  RecordCycleCountCommand,
} from '../inventory-api.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface AccountDto {
  id: string;
  name: string;
}

interface CycleCountItemForm {
  inventoryItem: RepackableInventoryDto;
  countedQuantity: FormControl<number | null>;
}

@Component({
  selector: 'app-cycle-counting',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DecimalPipe,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatAutocompleteModule,
    MatListModule,
    MatDividerModule,
  ],
  templateUrl: './cycle-counting.component.html',
  styleUrls: ['./cycle-counting.component.scss'],
})
export class CycleCountingComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private inventoryApi = inject(InventoryApiService);
  private snackBar = inject(MatSnackBar);

  accounts = signal<AccountDto[]>([]);
  filteredAccounts$ = signal<AccountDto[]>([]);
  accountInventories = signal<RepackableInventoryDto[]>([]);
  filteredInventories$ = signal<RepackableInventoryDto[]>([]);

  isLoadingLookups = signal(true);
  isLoadingInventory = signal(false);
  isSubmitting = signal(false);

  cycleCountForm: FormGroup;

  accountSearchCtrl = new FormControl<string | AccountDto | null>(null);
  inventorySearchCtrl = new FormControl<string | RepackableInventoryDto | null>(
    null
  );

  constructor() {
    this.cycleCountForm = this.fb.group({
      accountId: ['', Validators.required],
      durationHours: [null, [Validators.required, Validators.min(0.1)]],
      countedItems: this.fb.array([], Validators.required),
    });
  }

  ngOnInit(): void {
    this.loadAccounts();
    this.setupAccountAutocomplete();
    this.setupInventoryAutocomplete();
  }

  get countedItems(): FormArray {
    return this.cycleCountForm.get('countedItems') as FormArray;
  }

  createCountedItemGroup(item: RepackableInventoryDto): FormGroup {
    return this.fb.group({
      inventoryItem: [item],
      countedQuantity: [
        null as number | null,
        [Validators.required, Validators.min(0)],
      ],
    });
  }

  addItemToCount(event: MatAutocompleteSelectedEvent): void {
    const selectedItem: RepackableInventoryDto = event.option.value;

    const existingIndex = this.countedItems.controls.findIndex(
      (control) =>
        control.value.inventoryItem.inventoryId === selectedItem.inventoryId
    );

    if (existingIndex === -1 && selectedItem) {
      this.countedItems.push(this.createCountedItemGroup(selectedItem));
      this.inventorySearchCtrl.setValue('');

      this.filteredInventories$.set(this.accountInventories());
    } else {
      this.snackBar.open('Item already added to the count list.', 'Close', {
        duration: 2000,
      });
      this.inventorySearchCtrl.setValue('');
    }

    event.option.deselect();
  }

  removeItem(index: number): void {
    this.countedItems.removeAt(index);
  }

  loadAccounts(): void {
    this.http
      .get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe((data) => {
        this.accounts.set(data);
        this.filteredAccounts$.set(data);
        this.isLoadingLookups.set(false);
      });
  }

  setupAccountAutocomplete(): void {
    this.accountSearchCtrl.valueChanges
      .pipe(
        startWith(''),
        map((value) => (typeof value === 'string' ? value : value?.name)),
        map((name) =>
          name ? this._filterAccounts(name) : this.accounts().slice()
        )
      )
      .subscribe((filtered) => this.filteredAccounts$.set(filtered));
  }

  setupInventoryAutocomplete(): void {
    this.inventorySearchCtrl.valueChanges
      .pipe(
        startWith(''),
        map((value) =>
          typeof value === 'string' ? value : this.displayInventory(value)
        ),
        map((searchTerm) =>
          searchTerm
            ? this._filterInventory(searchTerm)
            : this.accountInventories().slice()
        )
      )
      .subscribe((filtered) => this.filteredInventories$.set(filtered));
  }

  private _filterAccounts(value: string): AccountDto[] {
    const filterValue = value.toLowerCase();
    return this.accounts().filter((acc) =>
      acc.name.toLowerCase().includes(filterValue)
    );
  }

  private _filterInventory(value: string): RepackableInventoryDto[] {
    const filterValue = value.toLowerCase();
    return this.accountInventories().filter(
      (inv) =>
        inv.materialName.toLowerCase().includes(filterValue) ||
        inv.sku.toLowerCase().includes(filterValue) ||
        inv.palletBarcode.toLowerCase().includes(filterValue) ||
        inv.location.toLowerCase().includes(filterValue)
    );
  }

  displayAccount(account: AccountDto): string {
    return account?.name || '';
  }
  displayInventory(inv: RepackableInventoryDto | null): string {
    return inv
      ? `${inv.materialName} (Pallet: ${inv.palletBarcode}, Loc: ${inv.location})`
      : '';
  }

  onAccountSelected(event: MatAutocompleteSelectedEvent): void {
    const selectedAccount: AccountDto = event.option.value;
    this.cycleCountForm.patchValue({ accountId: selectedAccount.id });
    this.countedItems.clear();
    this.inventorySearchCtrl.reset();
    this.inventorySearchCtrl.enable();
    this.loadAccountInventory(selectedAccount.id);
  }

  loadAccountInventory(accountId: string): void {
    this.isLoadingInventory.set(true);
    this.accountInventories.set([]);
    this.filteredInventories$.set([]);
    this.inventoryApi.getRepackableInventory(accountId).subscribe({
      next: (data) => {
        this.accountInventories.set(data);
        this.filteredInventories$.set(data);
        this.isLoadingInventory.set(false);
      },
      error: () => {
        this.snackBar.open(`Failed to load inventory for account.`, 'Close');
        this.isLoadingInventory.set(false);
      },
    });
  }

  onSubmit(): void {
    if (this.cycleCountForm.invalid) {
      this.snackBar.open(
        'Please fill in all required fields (Account, Duration, and Counted Quantities).',
        'Close',
        { duration: 4000 }
      );
      this.cycleCountForm.markAllAsTouched();
      return;
    }
    if (this.countedItems.length === 0) {
      this.snackBar.open(
        'Please add at least one inventory item to count.',
        'Close',
        { duration: 3000 }
      );
      return;
    }

    this.isSubmitting.set(true);

    const command: RecordCycleCountCommand = {
      durationHours: this.cycleCountForm.value.durationHours,
      countedItems: this.countedItems.value.map((item: any) => ({
        inventoryId: item.inventoryItem.inventoryId,
        countedQuantity: item.countedQuantity,
      })),
    };

    this.inventoryApi.recordCycleCount(command).subscribe({
      next: () => {
        this.snackBar.open('Cycle count recorded successfully!', 'OK', {
          duration: 3000,
        });

        this.countedItems.clear();
        this.cycleCountForm.patchValue({ durationHours: null });

        this.accountInventories.set([]);
        this.filteredInventories$.set([]);
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.snackBar.open(
          `Error recording cycle count: ${err.error?.title || 'Unknown error'}`,
          'Close',
          { duration: 5000 }
        );
        this.isSubmitting.set(false);
      },
    });
  }
}
