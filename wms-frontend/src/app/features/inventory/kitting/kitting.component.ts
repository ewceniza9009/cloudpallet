// ---- File: wms-frontend/src/app/features/inventory/kitting/kitting.component.ts [FIXED] ----

import { Component, OnInit, inject, signal, computed, effect } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select'; // <-- *** ADD THIS IMPORT ***
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AdminApiService, BomDto, BomLineDto, MaterialDetailDto } from '../../admin/admin-api.service';
import { InventoryApiService, MaterialDto, RepackableInventoryDto, CreateKitCommand } from '../inventory-api.service';
import { map, startWith, switchMap, tap, of, catchError, Observable } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';

// DTOs for lookups
interface AccountDto { id: string; name: string; }
// --- FIX: Use the full MaterialDetailDto and add the bom property ---
type KitMaterialDto = MaterialDetailDto & { bom: BomDto };

@Component({
  selector: 'app-kitting',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, DecimalPipe, MatCardModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatIconModule, MatSnackBarModule,
    MatProgressSpinnerModule, MatAutocompleteModule, MatListModule, MatDividerModule,
    MatSelectModule // <-- *** ADD THE MODULE HERE ***
  ],
  templateUrl: './kitting.component.html',
  styleUrls: ['./kitting.component.scss']
})
export class KittingComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private inventoryApi = inject(InventoryApiService);
  private adminApi = inject(AdminApiService);
  private snackBar = inject(MatSnackBar);

  // --- Signals for Data ---
  accounts = signal<AccountDto[]>([]);
  allMaterials = signal<MaterialDetailDto[]>([]); // For getting component names
  kitMaterials = signal<KitMaterialDto[]>([]); // All materials that HAVE a BOM
  accountInventories = signal<RepackableInventoryDto[]>([]); // All inventory for the selected account

  // --- Signals for State ---
  isLoadingLookups = signal(true);
  isLoadingInventory = signal(false);
  isSubmitting = signal(false);

  // --- Form ---
  kittingForm: FormGroup;
  accountSearchCtrl = new FormControl<AccountDto | string | null>(null, Validators.required);
  kitMaterialSearchCtrl = new FormControl<KitMaterialDto | string | null>({ value: null, disabled: true }, Validators.required);

  // --- Autocomplete Signals ---
  filteredAccounts = signal<AccountDto[]>([]);
  filteredKitMaterials = signal<KitMaterialDto[]>([]);

  constructor() {
    this.kittingForm = this.fb.group({
      accountId: ['', Validators.required],
      targetKitMaterialId: ['', Validators.required],
      quantityToBuild: [1, [Validators.required, Validators.min(1)]],
      durationHours: [null, [Validators.required, Validators.min(0.1)]],
      components: this.fb.array([], Validators.required) // This will hold the selected source items
    });

    // --- EFFECT for Account Autocomplete ---
    effect(() => {
      const query = this.accountSearchCtrl.value;
      const accounts = this.accounts();
      const filterValue = (typeof query === 'string' ? query : query?.name || '').toLowerCase();
      this.filteredAccounts.set(
        accounts.filter(acc => acc.name.toLowerCase().includes(filterValue))
      );
    });

    // --- EFFECT for Kit Material Autocomplete ---
    effect(() => {
      const query = this.kitMaterialSearchCtrl.value;
      const kits = this.kitMaterials();
      const filterValue = (typeof query === 'string' ? query : query?.name || '').toLowerCase();
      this.filteredKitMaterials.set(
        kits.filter(mat => mat.name.toLowerCase().includes(filterValue) || mat.sku.toLowerCase().includes(filterValue))
      );
    });
  }

  ngOnInit(): void {
    this.loadInitialLookups();
  }

  // --- FormArray Accessor ---
  get components(): FormArray {
    return this.kittingForm.get('components') as FormArray;
  }

  // --- Data Loading ---
  async loadInitialLookups(): Promise<void> {
    this.isLoadingLookups.set(true);
    try {
      const accounts = await this.http.get<AccountDto[]>(`${environment.apiUrl}/Lookups/accounts`).toPromise();
      this.accounts.set(accounts || []);

      const allMaterials = await this.adminApi.getMaterials().toPromise();
      if (!allMaterials) {
        this.isLoadingLookups.set(false);
        return;
      }
      this.allMaterials.set(allMaterials); // Store all materials

      // Find all materials that have a BOM (i.e., are "Kits")
      const kitMaterialPromises = allMaterials.map(async (material) => {
        try {
          const bom = await this.adminApi.getBomForMaterial(material.id).toPromise();
          return bom ? { ...material, bom } : null; // Only return if BOM exists
        } catch {
          return null; // 404 Not Found
        }
      });

      const kits = (await Promise.all(kitMaterialPromises))
        .filter((kit): kit is KitMaterialDto => kit !== null && (kit.bom.lines.length > 0) && (kit.materialType === 'Kit' || kit.materialType === 'Repack'));

      this.kitMaterials.set(kits);

    } catch (err) {
      this.snackBar.open('Failed to load accounts or kit definitions.', 'Close', { duration: 5000 });
    } finally {
      this.isLoadingLookups.set(false);
    }
  }

  // --- Event Handlers ---
  onAccountSelected(event: MatAutocompleteSelectedEvent): void {
    const account: AccountDto = event.option.value;
    this.kittingForm.patchValue({ accountId: account.id });
    this.resetKitSelection(); // Clear kit and components
    this.kitMaterialSearchCtrl.enable();
    this.loadAccountInventory(account.id);
  }

  onKitMaterialSelected(event: MatAutocompleteSelectedEvent): void {
    const kit: KitMaterialDto = event.option.value;
    this.kittingForm.patchValue({ targetKitMaterialId: kit.id });
    this.buildComponentForm(kit.bom);
  }

  loadAccountInventory(accountId: string): void {
    this.isLoadingInventory.set(true);
    this.inventoryApi.getRepackableInventory(accountId).subscribe({
      next: (data) => {
        this.accountInventories.set(data);
        this.isLoadingInventory.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load inventory for this account.', 'Close', { duration: 4000 });
        this.isLoadingInventory.set(false);
      }
    });
  }

  // --- Dynamic Form Building ---
  buildComponentForm(bom: BomDto): void {
    this.components.clear();
    bom.lines.forEach(line => {
      const availableSources = this.accountInventories().filter(inv => inv.materialId === line.inputMaterialId);

      const componentGroup = this.fb.group({
        bomLine: [line],
        componentMaterialName: [this.allMaterials().find(m => m.id === line.inputMaterialId)?.name || 'Unknown Material'],
        availableSources: [availableSources],
        sourceInventoryId: [null, Validators.required],
        quantityToConsume: [bom.outputQuantity * line.inputQuantity, Validators.required] // Pre-calculate quantity
      });

      // When quantityToBuild changes, update all component quantities
      this.kittingForm.get('quantityToBuild')!.valueChanges.pipe(
        startWith(this.kittingForm.get('quantityToBuild')!.value)
      ).subscribe(qty => {
        const qtyToConsume = (qty || 0) * line.inputQuantity;
        componentGroup.get('quantityToConsume')?.setValue(qtyToConsume);
      });

      this.components.push(componentGroup);
    });
  }

  resetKitSelection(): void {
    this.kitMaterialSearchCtrl.reset();
    this.kittingForm.patchValue({ targetKitMaterialId: '' });
    this.components.clear();
  }

  // --- Autocomplete Filtering & Display ---
  private _filterAccounts(value: string): AccountDto[] {
    const filterValue = value.toLowerCase();
    return this.accounts().filter(acc => acc.name.toLowerCase().includes(filterValue));
  }
  private _filterKitMaterials(value: string): KitMaterialDto[] {
    const filterValue = value.toLowerCase();
    return this.kitMaterials().filter(mat => mat.name.toLowerCase().includes(filterValue) || mat.sku.toLowerCase().includes(filterValue));
  }
  displayAccount(account: AccountDto): string { return account?.name || ''; }
  displayMaterial(mat: KitMaterialDto): string { return mat ? `${mat.name} (${mat.sku})` : ''; }

  // --- Form Submission ---
  onSubmit(): void {
    if (this.kittingForm.invalid) {
      this.snackBar.open('Please fill in all fields and select a source for each component.', 'Close', { duration: 4000 });
      this.kittingForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const formValue = this.kittingForm.value;

    const command: CreateKitCommand = {
      targetKitMaterialId: formValue.targetKitMaterialId,
      quantityToBuild: formValue.quantityToBuild,
      durationHours: formValue.durationHours,
      components: formValue.components.map((comp: any) => ({
        componentMaterialId: comp.bomLine.inputMaterialId,
        sourceInventoryId: comp.sourceInventoryId,
        quantityToConsume: comp.quantityToConsume
      }))
    };

    this.inventoryApi.createKit(command).subscribe({
      next: (newInventoryId: string) => {
        this.snackBar.open(`Successfully built ${formValue.quantityToBuild} kits! New inventory ID: ${newInventoryId}`, 'OK', { duration: 5000 });
        this.isSubmitting.set(false);
        // Reset form
        this.resetKitSelection();
        this.accountSearchCtrl.reset();
        this.kitMaterialSearchCtrl.disable();
        this.kittingForm.reset({ quantityToBuild: 1 });
        this.accountInventories.set([]);
      },
      error: (err: any) => {
        this.snackBar.open(`Error: ${err.error?.title || 'Failed to create kit.'}`, 'Close', { duration: 7000 });
        this.isSubmitting.set(false);
      }
    });
  }
}
