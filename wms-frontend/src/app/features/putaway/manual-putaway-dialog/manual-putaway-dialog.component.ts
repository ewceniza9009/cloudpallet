import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { InventoryApiService, LocationDto } from '../../inventory/inventory-api.service';
import { map, startWith } from 'rxjs';

@Component({
  selector: 'app-manual-putaway-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './manual-putaway-dialog.component.html',
  styleUrls: ['./manual-putaway-dialog.component.scss']
})
export class ManualPutawayDialogComponent implements OnInit {
  private fb = inject(FormBuilder);
  private inventoryApi = inject(InventoryApiService);
  public dialogRef = inject(MatDialogRef<ManualPutawayDialogComponent>);

  locations = signal<LocationDto[]>([]);
  filteredLocations = signal<LocationDto[]>([]);
  isLoading = signal(true);

  form: FormGroup;

  constructor() {
    this.form = this.fb.group({
      locationId: ['', Validators.required],
      locationSearch: ['']
    });
  }

  ngOnInit(): void {
    this.inventoryApi.getAvailableStorageLocations().subscribe({
      next: (data) => {
        this.locations.set(data);
        this.isLoading.set(false);
        this.setupFilter();
      },
      error: () => this.isLoading.set(false)
    });
  }

  private setupFilter(): void {
    this.form.get('locationSearch')?.valueChanges.pipe(
      startWith(''),
      map(value => this._filter(value || ''))
    ).subscribe(filtered => this.filteredLocations.set(filtered));
  }

  private _filter(value: string): LocationDto[] {
    const filterValue = value.toLowerCase();
    return this.locations().filter(loc => loc.displayName.toLowerCase().includes(filterValue));
  }

  displayLocationName(location: LocationDto): string {
    return location?.displayName || '';
  }

  onLocationSelected(location: LocationDto): void {
    this.form.get('locationId')?.setValue(location.id);
  }

  onConfirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value.locationId);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
