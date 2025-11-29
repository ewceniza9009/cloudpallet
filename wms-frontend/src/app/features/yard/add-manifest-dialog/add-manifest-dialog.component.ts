import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { YardApiService, CargoManifestDto, CreateCargoManifestCommand } from '../yard-api.service';
import { Observable, of } from 'rxjs';
import { map, startWith, switchMap } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

export interface MaterialDto {
  id: string;
  name: string;
}

@Component({
  selector: 'app-add-manifest-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatListModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './add-manifest-dialog.component.html',
  styleUrls: ['./add-manifest-dialog.component.scss']
})
export class AddManifestDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private yardApi = inject(YardApiService);
  private http = inject(HttpClient);
  private snackBar = inject(MatSnackBar);
  public dialogRef = inject(MatDialogRef<AddManifestDialogComponent>);
  public data = inject(MAT_DIALOG_DATA);

  appointmentId: string = this.data.appointmentId;
  existingManifest = signal<CargoManifestDto | null>(null);
  isLoading = signal(true);
  isSaving = signal(false);

  form: FormGroup = this.fb.group({
    lines: this.fb.array([])
  });

  materials: MaterialDto[] = [];
  filteredMaterials: Observable<MaterialDto[]>[] = [];

  get lines() {
    return this.form.get('lines') as FormArray;
  }

  ngOnInit(): void {
    this.loadMaterials().subscribe(() => {
      this.loadManifest();
    });
  }

  loadMaterials(): Observable<void> {
    return this.http.get<MaterialDto[]>(`${environment.apiUrl}/Lookups/materials`).pipe(
      map(materials => {
        this.materials = materials;
      })
    );
  }

  loadManifest(): void {
    this.yardApi.getManifest(this.appointmentId).subscribe({
      next: (manifest) => {
        this.existingManifest.set(manifest);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.addLine();
      }
    });
  }

  addLine(): void {
    const lineGroup = this.fb.group({
      material: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]]
    });

    this.lines.push(lineGroup);
    this.manageAutocomplete(this.lines.length - 1);
  }

  removeLine(index: number): void {
    this.lines.removeAt(index);
    this.filteredMaterials.splice(index, 1);
  }

  manageAutocomplete(index: number) {
    const control = this.lines.at(index).get('material');
    if (control) {
        this.filteredMaterials[index] = control.valueChanges.pipe(
            startWith(''),
            map(value => {
                const name = typeof value === 'string' ? value : value?.name;
                return name ? this._filter(name as string) : this.materials.slice();
            })
        );
    }
  }

  private _filter(name: string): MaterialDto[] {
    const filterValue = name.toLowerCase();
    return this.materials.filter(option => option.name.toLowerCase().includes(filterValue));
  }

  displayFn(material: MaterialDto): string {
    return material && material.name ? material.name : '';
  }

  save(): void {
    if (this.form.invalid) return;

    this.isSaving.set(true);
    const formValue = this.form.value;
    
    const command: CreateCargoManifestCommand = {
        appointmentId: this.appointmentId,
        lines: formValue.lines.map((l: any) => ({
            materialId: l.material.id,
            expectedQuantity: l.quantity
        }))
    };

    this.yardApi.createManifest(command).subscribe({
        next: () => {
            this.snackBar.open('Manifest created successfully', 'OK', { duration: 3000 });
            this.dialogRef.close(true);
        },
        error: (err) => {
            this.snackBar.open('Failed to create manifest', 'Close', { duration: 5000 });
            this.isSaving.set(false);
        }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
