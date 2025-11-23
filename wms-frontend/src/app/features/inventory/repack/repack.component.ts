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
import { map, startWith, switchMap, tap, of, catchError } from 'rxjs';
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

  private accountInventories = signal<RepackableInventoryDto[]>([]);

  displayableSourceInventories = signal<RepackableInventoryDto[]>([]);

  filteredAccounts$!: Observable<AccountDto[]>;
  filteredTargetMaterials$!: Observable<MaterialDto[]>;

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
  }

  onAccountSelected(account: AccountDto): void {
    this.repackForm.get('targetMaterial')?.reset();
    this.repackForm.get('sourceInventory')?.reset();
    this.repackForm.get('sourceInventory')?.disable();
    this.displayableSourceInventories.set([]);

    if (account?.id) {
      this.isLoadingSources.set(true);
      this.repackForm.get('targetMaterial')?.enable();

      this.inventoryApi
        .getRepackableInventory(account.id)
        .subscribe((inventories) => {
          this.accountInventories.set(inventories);
          this.isLoadingSources.set(false);
        });
    }
  }

  onTargetMaterialSelected(targetMaterial: MaterialDto): void {
    this.repackForm.get('sourceInventory')?.reset();
    this.repackForm.get('sourceInventory')?.disable();
    this.displayableSourceInventories.set([]);

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
          const validSources = this.accountInventories().filter(
            (inv) => inv.materialId === requiredInputId
          );

          if (validSources.length > 0) {
            this.displayableSourceInventories.set(validSources);
            this.repackForm.get('sourceInventory')?.enable();
          } else {
            this.snackBar.open(
              `No stock of the required input material was found for this account.`,
              'Close',
              { duration: 4000 }
            );
          }
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
