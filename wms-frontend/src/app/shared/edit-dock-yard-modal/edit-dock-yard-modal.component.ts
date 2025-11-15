import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DockSetupDto, YardSpotSetupDto } from '../../features/admin/admin-setup-api.service';

export type EditModalData = {
  type: 'dock' | 'yardSpot';
  item?: DockSetupDto | YardSpotSetupDto;
};

@Component({
  selector: 'app-edit-dock-yard-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule
  ],
  template: `
    <div class="modal-container">
      <h2 mat-dialog-title class="modal-title">
        <mat-icon>{{ data.type === 'dock' ? 'dock' : 'local_parking' }}</mat-icon>
        {{ data.item ? 'Edit' : 'Add New' }} {{ data.type === 'dock' ? 'Dock' : 'Yard Spot' }}
      </h2>

      <div mat-dialog-content class="modal-content">
        <form [formGroup]="editForm" class="edit-form">
          <mat-form-field appearance="outline" *ngIf="data.type === 'dock'">
            <mat-label>Dock Name</mat-label>
            <input matInput formControlName="name" placeholder="Enter dock name" />
            <mat-error *ngIf="editForm.get('name')?.hasError('required')">
              Dock name is required
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" *ngIf="data.type === 'yardSpot'">
            <mat-label>Spot Number</mat-label>
            <input matInput formControlName="spotNumber" placeholder="Enter spot number" />
            <mat-error *ngIf="editForm.get('spotNumber')?.hasError('required')">
              Spot number is required
            </mat-error>
          </mat-form-field>

          <mat-slide-toggle
            formControlName="isActive"
            color="accent"
            *ngIf="data.type === 'yardSpot'">
            Active
          </mat-slide-toggle>
        </form>
      </div>

      <div mat-dialog-actions class="modal-actions">
        <button mat-button (click)="onCancel()" type="button">Cancel</button>
        <button
          mat-flat-button
          color="primary"
          (click)="onSave()"
          [disabled]="editForm.invalid"
          type="button">
          <mat-icon>{{ data.item ? 'save' : 'add' }}</mat-icon>
          {{ data.item ? 'Update' : 'Create' }}
        </button>
      </div>
    </div>
  `,
  styleUrls: ['./edit-dock-yard-modal.component.scss']
})
export class EditDockYardModalComponent {
  private fb = inject(FormBuilder);
  public dialogRef = inject(MatDialogRef<EditDockYardModalComponent>);

  editForm: FormGroup;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: EditModalData
  ) {
    this.editForm = this.createForm();

    if (data.item) {
      this.editForm.patchValue(data.item);
    }
  }

  private createForm(): FormGroup {
    if (this.data.type === 'dock') {
      return this.fb.group({
        name: ['', Validators.required]
      });
    } else {
      return this.fb.group({
        spotNumber: ['', Validators.required],
        isActive: [true]
      });
    }
  }

  onSave(): void {
    if (this.editForm.valid) {
      this.dialogRef.close(this.editForm.value);
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
