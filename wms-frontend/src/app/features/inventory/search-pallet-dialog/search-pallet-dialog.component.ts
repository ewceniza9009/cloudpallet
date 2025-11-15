import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  MatListModule,
  MatListOption,
  MatSelectionListChange,
} from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { HttpClient } from '@angular/common/http';
import {
  Observable,
  Subject,
  debounceTime,
  map,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  InventoryApiService,
  StoredPalletSearchResultDto,
} from '../inventory-api.service';

interface AccountDto {
  id: string;
  name: string;
}
interface MaterialDto {
  id: string;
  name: string;
  sku: string;
}

@Component({
  selector: 'app-search-pallet-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    MatListModule,
    MatIconModule,
  ],
  templateUrl: './search-pallet-dialog.component.html',
  styleUrls: ['./search-pallet-dialog.component.scss'],
})
export class SearchPalletDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private inventoryApi = inject(InventoryApiService);
  public dialogRef = inject(MatDialogRef<SearchPalletDialogComponent>);

  accounts = signal<AccountDto[]>([]);
  materials = signal<MaterialDto[]>([]);
  filteredAccounts$: Observable<AccountDto[]>;
  filteredMaterials$: Observable<MaterialDto[]>;

  searchResults = signal<StoredPalletSearchResultDto[]>([]);
  isLoadingLookups = signal(true);
  isSearching = signal(false);
  selectedPallet = signal<StoredPalletSearchResultDto | null>(null);

  accountControl = new FormControl<AccountDto | string | null>(null);
  materialControl = new FormControl<MaterialDto | string | null>(null);
  barcodeControl = new FormControl<string | null>(null);

  private searchTrigger = new Subject<void>();

  constructor() {
    this.filteredAccounts$ = this.accountControl.valueChanges.pipe(
      startWith(''),
      map((value) => (typeof value === 'string' ? value : value?.name) || ''),
      map((name) =>
        name ? this._filterAccounts(name) : this.accounts().slice()
      )
    );

    this.filteredMaterials$ = this.materialControl.valueChanges.pipe(
      startWith(''),
      map((value) => (typeof value === 'string' ? value : value?.name) || ''),
      map((name) =>
        name ? this._filterMaterials(name) : this.materials().slice()
      )
    );
  }

  ngOnInit(): void {
    this.loadLookups();
    this.setupSearchListener();
  }

  loadLookups(): void {
    this.isLoadingLookups.set(true);
    const accounts$ = this.http.get<AccountDto[]>(
      `${environment.apiUrl}/Lookups/accounts`
    );
    const materials$ = this.http.get<MaterialDto[]>(
      `${environment.apiUrl}/Lookups/materials`
    );

    Promise.all([accounts$.toPromise(), materials$.toPromise()]).then(
      ([accounts, materials]) => {
        this.accounts.set(accounts || []);
        this.materials.set(materials || []);
        this.isLoadingLookups.set(false);

        this.searchTrigger.next();
      }
    );
  }

  setupSearchListener(): void {
    this.searchTrigger
      .pipe(
        debounceTime(400),
        tap(() => this.isSearching.set(true)),
        switchMap(() => {
          const accountId =
            (typeof this.accountControl.value === 'object'
              ? this.accountControl.value?.id
              : null) || undefined;
          const materialId =
            (typeof this.materialControl.value === 'object'
              ? this.materialControl.value?.id
              : null) || undefined;
          const barcodeQuery = this.barcodeControl.value || undefined;

          return this.inventoryApi.searchStoredPallets(
            accountId,
            materialId,
            barcodeQuery
          );
        })
      )
      .subscribe((results) => {
        this.searchResults.set(results);
        this.isSearching.set(false);
      });
  }

  onFilterChange(): void {
    this.searchTrigger.next();
  }

  onSelectionChange(event: MatSelectionListChange): void {
    this.selectedPallet.set(event.options[0].value);
  }

  displayAccountName(account: AccountDto): string {
    return account?.name || '';
  }
  displayMaterialName(material: MaterialDto): string {
    return material ? `${material.name} (${material.sku})` : '';
  }

  private _filterAccounts(value: string): AccountDto[] {
    const filterValue = value.toLowerCase();
    return this.accounts().filter((acc) =>
      acc.name.toLowerCase().includes(filterValue)
    );
  }
  private _filterMaterials(value: string): MaterialDto[] {
    const filterValue = value.toLowerCase();
    return this.materials().filter(
      (mat) =>
        mat.name.toLowerCase().includes(filterValue) ||
        mat.sku.toLowerCase().includes(filterValue)
    );
  }

  onConfirm(): void {
    if (this.selectedPallet()) {
      this.dialogRef.close(this.selectedPallet()!.palletBarcode);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
