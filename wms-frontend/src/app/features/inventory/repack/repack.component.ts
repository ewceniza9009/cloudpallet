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
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminApiService, BomDto } from '../../admin/admin-api.service';
import {
  InventoryApiService,
  MaterialDto,
  RepackableInventoryDto,
} from '../inventory-api.service';
import { map, startWith, switchMap, tap, of, catchError, debounceTime, distinctUntilChanged } from 'rxjs';
import { Observable } from 'rxjs';
import { ScrollingModule } from '@angular/cdk/scrolling';

interface AccountDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-repack',
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
    MatAutocompleteModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    ScrollingModule,
  ],
  templateUrl: './repack.component.html',
  styleUrls: ['./repack.component.scss'],
})
export class RepackComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private inventoryApi = inject(InventoryApiService);
  private adminApi = inject(AdminApiService);
  private snackBar = inject(MatSnackBar);

  repackForm: FormGroup;
  accounts = signal<AccountDto[]>([]);
  allMaterials = signal<MaterialDto[]>([]);

  requiredInputMaterialId = signal<string | null>(null);

  filteredAccounts$!: Observable<AccountDto[]>;
  filteredTargetMaterials$!: Observable<MaterialDto[]>;
  filteredSourceInventories$!: Observable<RepackableInventoryDto[]>;

  isSubmitting = signal(false);
  isLoadingSources = signal(false);

  constructor() {
    this.repackForm = this.fb.group({
      account: [null, Validators.required],
      sourceInventory: [{ value: null, disabled: true }, Validators.required],
      targetMaterial: [{ value: null, disabled: true }, Validators.required],
      quantityToProcess: [1, [Validators.required, Validators.min(1)]],
      durationHours: [null, [Validators.required, Validators.min(0.1)]],
    });
  }

  ngOnInit(): void {
    this.loadInitialLookups();
    this.setupFilters();
  }

  loadInitialLookups(): void {
    this.http
      .get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`)
      .subscribe((data) => this.accounts.set(data));
    this.http
      .get<MaterialDto[]>(`${environment.apiUrl}/Lookups/materials`)
      .subscribe((data) => this.allMaterials.set(data));
  }

  setupFilters(): void {
    this.filteredAccounts$ = this.repackForm.get('account')!.valueChanges.pipe(
      startWith(''),
      map((value) => this._filterAccounts(value))
    );
    this.filteredTargetMaterials$ = this.repackForm
      .get('targetMaterial')!
      .valueChanges.pipe(
        startWith(''),
        map((value) => this._filterMaterials(value))
      );

    this.filteredSourceInventories$ = this.repackForm.get('sourceInventory')!.valueChanges.pipe(
      startWith(''),
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((value) => {
        const accountId = this.repackForm.get('account')?.value?.id;
        const materialId = this.requiredInputMaterialId();
        
        if (!accountId || !materialId) return of([]);

        const searchTerm = typeof value === 'string' ? value : '';
        this.isLoadingSources.set(true);

        return this.inventoryApi.getRepackableInventory(accountId, materialId, searchTerm, 1, 20).pipe(
          map(result => result.items),
          tap(() => this.isLoadingSources.set(false)),
          catchError(() => {
            this.isLoadingSources.set(false);
            return of([]);
          })
        );
      })
    );
  }

  onAccountSelected(account: AccountDto): void {
    this.repackForm.get('targetMaterial')?.reset();
    this.repackForm.get('sourceInventory')?.reset();
    this.repackForm.get('sourceInventory')?.disable();
    this.requiredInputMaterialId.set(null);

    if (account?.id) {
      this.repackForm.get('targetMaterial')?.enable();
    }
  }

  onTargetMaterialSelected(targetMaterial: MaterialDto): void {
    this.repackForm.get('sourceInventory')?.reset();
    this.repackForm.get('sourceInventory')?.disable();
    this.requiredInputMaterialId.set(null);

    if (!targetMaterial?.id) return;

    this.isLoadingSources.set(true);
    this.adminApi
      .getBomForMaterial(targetMaterial.id)
      .pipe(
        catchError(() => {
          this.snackBar.open(
            `No repackaging recipe found for ${targetMaterial.name}.`,
            'Close',
            { duration: 4000 }
          );
          return of(null);
        })
      )
      .subscribe((bom) => {
        this.isLoadingSources.set(false);
        if (bom && bom.lines.length > 0) {
          const requiredInputId = bom.lines[0].inputMaterialId;
          this.requiredInputMaterialId.set(requiredInputId);
          this.repackForm.get('sourceInventory')?.enable();
          // Trigger initial load
          this.repackForm.get('sourceInventory')?.updateValueAndValidity({ emitEvent: true });
        } else {
          this.snackBar.open(
            `No stock of the required input material was found for this account.`,
            'Close',
            { duration: 4000 }
          );
        }
      });
  }

  private _filterAccounts(value: string | AccountDto): AccountDto[] {
    const filterValue = (
      typeof value === 'string' ? value : value?.name || ''
    ).toLowerCase();
    return this.accounts().filter((acc) =>
      acc.name.toLowerCase().includes(filterValue)
    );
  }

  private _filterMaterials(value: string | MaterialDto): MaterialDto[] {
    const filterValue = (
      typeof value === 'string' ? value : value?.name || ''
    ).toLowerCase();
    return this.allMaterials().filter(
      (mat) =>
        mat.name.toLowerCase().includes(filterValue) ||
        mat.sku.toLowerCase().includes(filterValue)
    );
  }

  displayAccount(account: AccountDto): string {
    return account?.name || '';
  }
  displaySource(inv: RepackableInventoryDto): string {
    return inv ? `${inv.materialName} (${inv.palletBarcode})` : '';
  }
  displayMaterial(mat: MaterialDto): string {
    return mat?.name || '';
  }

  get selectedSourceInventory(): RepackableInventoryDto | null {
    const value = this.repackForm.get('sourceInventory')?.value;
    if (value && typeof value === 'object' && 'inventoryId' in value) {
      return value as RepackableInventoryDto;
    }
    return null;
  }

  onSubmit(): void {
    if (this.repackForm.invalid) return;
    this.isSubmitting.set(true);

    const formValue = this.repackForm.getRawValue();

    if (
      !formValue.sourceInventory ||
      typeof formValue.sourceInventory === 'string'
    ) {
      this.snackBar.open('Invalid source inventory selected.', 'Close');
      this.isSubmitting.set(false);
      return;
    }

    this.inventoryApi
      .recordRepack(
        formValue.sourceInventory.inventoryId,
        formValue.targetMaterial.id,
        formValue.quantityToProcess,
        formValue.durationHours
      )
      .subscribe({
        next: () => {
          this.snackBar.open(
            'Repackaging transaction recorded successfully!',
            'OK',
            { duration: 4000 }
          );
          this.repackForm.reset({ quantityToProcess: 1 });
          this.repackForm.get('targetMaterial')?.disable();
          this.repackForm.get('sourceInventory')?.disable();
          this.isSubmitting.set(false);
        },
        error: (err: any) => {
          this.snackBar.open(
            `Error: ${err.error?.title || 'Failed to record transaction.'}`,
            'Close'
          );
          this.isSubmitting.set(false);
        },
      });
  }
}
