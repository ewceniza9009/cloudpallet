// ---- File: wms-frontend/src/app/features/admin/material-management/material-detail/material-detail.component.ts [FINAL] ----

import { Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormArray,
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
import { MatDividerModule } from '@angular/material/divider';
import {
  AdminApiService,
  BomDto,
  CreateBillOfMaterialCommand,
  CreateMaterialCommand,
  MaterialDetailDto,
  UpdateMaterialCommand,
  MaterialType,
  BarcodeFormat,
} from '../../admin-api.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { EMPTY, catchError, filter, switchMap } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../../shared/confirmation-dialog/confirmation-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';

interface LookupDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-material-detail',
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
    MatDividerModule,
    MatTooltipModule,
  ],
  templateUrl: './material-detail.component.html',
  styleUrls: ['./material-detail.component.scss'],
})
export class MaterialDetailComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminApi = inject(AdminApiService);
  private http = inject(HttpClient);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  materialForm: FormGroup;
  bomForm: FormGroup;
  materialId = signal<string | null>(null);
  isEditMode = signal(false);
  isLoading = signal(true);
  isSaving = signal(false);
  isDeleting = signal(false);
  isSubmittingBom = signal(false);

  categories = signal<LookupDto[]>([]);
  uoms = signal<LookupDto[]>([]);
  allMaterials = signal<MaterialDetailDto[]>([]);
  bom = signal<BomDto | null>(null);

  materialTypes: MaterialType[] = ['Normal', 'Kit', 'Repack'];
  barcodeFormats: BarcodeFormat[] = ['GS1_128', 'UPC'];

  constructor() {
    this.materialForm = this.fb.group({
      name: ['', Validators.required],
      sku: ['', Validators.required],
      description: [''],
      categoryId: ['', Validators.required],
      uomId: ['', Validators.required],
      requiredTempZone: ['FrozenStorage', Validators.required],
      baseWeight: [0, [Validators.required, Validators.min(0)]],
      costPerUnit: [0, [Validators.required, Validators.min(0)]],
      materialType: ['Normal', Validators.required],
      perishable: [false],
      shelfLifeDays: [
        { value: 0, disabled: true },
        [Validators.required, Validators.min(0)],
      ],
      isHazardous: [false],
      gs1BarcodePrefix: [''],
      isActive: [true],
      defaultBarcodeFormat: ['GS1_128', Validators.required],
      dimensionsLength: [0, [Validators.required, Validators.min(0)]],
      dimensionsWidth: [0, [Validators.required, Validators.min(0)]],
      dimensionsHeight: [0, [Validators.required, Validators.min(0)]],
      minStockLevel: [0, [Validators.required, Validators.min(0)]],
      maxStockLevel: [0, [Validators.required, Validators.min(0)]],
      packageTareWeightPerUom: [0, [Validators.required, Validators.min(0)]],
    });

    this.bomForm = this.fb.group({
      outputQuantity: [1, [Validators.required, Validators.min(0.001)]],
      lines: this.fb.array([]),
    });

    effect(() => {
      const isPerishable = this.materialForm.get('perishable')?.value;
      const shelfLifeControl = this.materialForm.get('shelfLifeDays');
      if (isPerishable) {
        shelfLifeControl?.enable();
      } else {
        shelfLifeControl?.disable();
        shelfLifeControl?.setValue(0);
      }
    });
  }

  ngOnInit(): void {
    this.loadLookups();

    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (id && id !== 'new') {
            this.materialId.set(id);
            this.isEditMode.set(false);
            this.materialForm.disable();
            return this.adminApi.getMaterialById(id);
          } else {
            this.materialId.set(null);
            this.isEditMode.set(true);
            this.materialForm.enable();
            if (!this.materialForm.get('perishable')?.value) {
              this.materialForm.get('shelfLifeDays')?.disable();
            }
            this.isLoading.set(false);
            this.addBomLine();
            return Promise.resolve(null);
          }
        })
      )
      .subscribe((material) => {
        if (material) {
          this.materialForm.patchValue(material);
          this.loadBom(material.id);
          if (!material.perishable) {
            this.materialForm.get('shelfLifeDays')?.disable();
          }
        }
        this.isLoading.set(false);
      });
  }

  get bomLines(): FormArray {
    return this.bomForm.get('lines') as FormArray;
  }

  get currentMaterialType(): MaterialType {
    return this.materialForm.get('materialType')?.value || 'Normal';
  }

  createBomLine(): FormGroup {
    return this.fb.group({
      inputMaterialId: ['', Validators.required],
      inputQuantity: [1, [Validators.required, Validators.min(0.001)]],
    });
  }

  addBomLine(): void {
    if (this.currentMaterialType === 'Repack' && this.bomLines.length >= 1) {
      this.snackBar.open(
        'A "Repack" material can only have one component.',
        'Close',
        { duration: 3000 }
      );
      return;
    }
    this.bomLines.push(this.createBomLine());
  }

  removeBomLine(index: number): void {
    if (this.bomLines.length > 1) {
      this.bomLines.removeAt(index);
    }
  }

  loadLookups(): void {
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/material-categories`)
      .subscribe((data) => this.categories.set(data));
    this.http
      .get<LookupDto[]>(`${environment.apiUrl}/Lookups/uoms`)
      .subscribe((data) => this.uoms.set(data));
    this.adminApi
      .getMaterials()
      .subscribe((data) => this.allMaterials.set(data));
  }

  loadBom(materialId: string): void {
    this.adminApi
      .getBomForMaterial(materialId)
      .pipe(
        catchError(() => {
          this.bom.set(null);
          this.addBomLine(); // Add an empty line if no BOM exists
          return EMPTY;
        })
      )
      .subscribe((bomData) => {
        this.bom.set(bomData);
        if (bomData) {
          this.bomForm.patchValue({
            outputQuantity: bomData.outputQuantity,
          });
          this.bomLines.clear();
          bomData.lines.forEach((line) => {
            this.bomLines.push(
              this.fb.group({
                inputMaterialId: [line.inputMaterialId, Validators.required],
                inputQuantity: [
                  line.inputQuantity,
                  [Validators.required, Validators.min(0.001)],
                ],
              })
            );
          });
        } else {
          this.bomLines.clear(); // Clear existing lines if BOM is null/empty
          this.addBomLine(); // Add a default empty line
        }
      });
  }

  toggleEditMode(isEditing: boolean): void {
    this.isEditMode.set(isEditing);
    if (isEditing) {
      this.materialForm.enable();
      if (this.materialId()) {
        this.materialForm.get('sku')?.disable();
        this.materialForm.get('materialType')?.disable();
      }
      if (!this.materialForm.get('perishable')?.value) {
        this.materialForm.get('shelfLifeDays')?.disable();
      }
    } else {
      this.materialForm.disable();
    }
  }

  save(): void {
    if (this.materialForm.invalid) {
      this.snackBar.open('Please fill in all required fields.', 'Close', {
        duration: 3000,
      });
      this.materialForm.markAllAsTouched(); // Show validation errors
      return;
    }
    this.isSaving.set(true);

    const formValue = this.materialForm.getRawValue();
    const needsBomSave =
      formValue.materialType !== 'Normal' &&
      this.bomLines.length > 0 &&
      this.bomForm.valid &&
      this.bomLines.at(0).get('inputMaterialId')?.value;

    if (this.materialId()) {
      // --- UPDATE PATH ---
      const command: UpdateMaterialCommand = {
        id: this.materialId()!,
        ...formValue,
      };
      this.adminApi.updateMaterial(this.materialId()!, command).subscribe({
        next: () => {
          this.handleSaveSuccess('Material updated successfully.');
          if (needsBomSave) {
            this.onBomSubmit(); // Save BOM after material
          } else {
            this.isSaving.set(false);
            this.toggleEditMode(false); // No BOM to save, just exit edit mode
          }
        },
        error: (err: any) => this.handleSaveError(err),
      });
    } else {
      // --- CREATE PATH ---
      const createCommand: CreateMaterialCommand = {
        ...formValue,
      };
      this.adminApi.createMaterial(createCommand).subscribe({
        next: (newId) => {
          this.handleSaveSuccess('Material created successfully.');
          this.materialId.set(newId); // Set the new ID
          if (needsBomSave) {
            this.onBomSubmit(); // Now save the BOM
          } else {
            this.isSaving.set(false);
            // Navigate to the new detail page
            this.router.navigate(['/admin/materials/detail', newId], {
              replaceUrl: true,
            });
          }
        },
        error: (err: any) => this.handleSaveError(err),
      });
    }
  }

  onBomSubmit(): void {
    if (this.bomForm.invalid || !this.materialId()) {
      // This case is for when the form is invalid, but not just empty
      this.snackBar.open(
        'Please fill out all required fields in the recipe.',
        'Close',
        { duration: 3000 }
      );
      this.bomForm.markAllAsTouched();
      this.isSaving.set(false); // Make sure to stop saving spinner
      return;
    }

    // Check for "empty but valid" BOM (e.g., one empty line)
    const formValue = this.bomForm.value;
    const commandLines = formValue.lines.filter(
      (line: any) => line.inputMaterialId && line.inputQuantity > 0
    );

    if (commandLines.length === 0) {
      // No valid lines to save, just finalize the Material save
      this.snackBar.open('Material saved. No BOM recipe was defined.', 'OK', { duration: 3000 });
      this.isSaving.set(false);
      if (!this.isEditMode()) { // Was a new creation
          this.router.navigate(['/admin/materials/detail', this.materialId()], { replaceUrl: true });
      } else { // Was an edit
          this.toggleEditMode(false);
      }
      return;
    }

    this.isSubmittingBom.set(true);

    const command: CreateBillOfMaterialCommand = {
      outputMaterialId: this.materialId()!,
      outputQuantity: formValue.outputQuantity,
      lines: commandLines
    };

    this.adminApi.createBillOfMaterial(command).subscribe({
      next: () => {
        this.snackBar.open('Recipe saved successfully!', 'OK', {
          duration: 3000,
        });
        this.loadBom(this.materialId()!); // Reload BOM data
        this.isSubmittingBom.set(false);
        this.isSaving.set(false); // Stop main spinner

        if (!this.isEditMode()) { // Was a new creation
            this.router.navigate(['/admin/materials/detail', this.materialId()], { replaceUrl: true });
        } else { // Was an edit
            this.toggleEditMode(false); // Exit edit mode
        }
      },
      error: (err: any) => {
        this.snackBar.open(
          `Error: ${
            err.error?.title || err.error?.detail || 'Failed to save recipe.'
          }`,
          'Close',
          { duration: 7000 }
        );
        this.isSubmittingBom.set(false);
        this.isSaving.set(false); // Stop main spinner
      },
    });
  }

  // This helper is now only responsible for the snackbar
  private handleSaveSuccess(message: string): void {

    this.router.navigate(['/admin/materials']);
    this.snackBar.open(message, 'OK', { duration: 3000 });
  }

  private handleSaveError(err: any): void {
    this.isSaving.set(false);
    this.snackBar.open(
      `Error: ${err.error?.title || 'Failed to save material.'}`,
      'Close'
    );
  }

  delete(): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Delete Material',
        message: `Are you sure you want to delete "${this.materialForm.value.name}"? This action cannot be undone.`,
      },
    });

    dialogRef
      .afterClosed()
      .pipe(filter((result: boolean) => result === true)) // Added boolean type
      .subscribe(() => {
        this.isDeleting.set(true);
        this.adminApi.deleteMaterial(this.materialId()!).subscribe({
          next: () => {
            this.snackBar.open('Material deleted successfully.', 'OK', {
              duration: 3000,
            });
            this.router.navigate(['/admin/materials']);
          },
          error: (err: any) => {
            this.snackBar.open(
              `Error: ${err.error?.title || 'Failed to delete material.'}`,
              'Close'
            );
            this.isDeleting.set(false);
          },
        });
      });
  }

  back(): void {
    this.router.navigate(['/admin/materials']);
  }
}
