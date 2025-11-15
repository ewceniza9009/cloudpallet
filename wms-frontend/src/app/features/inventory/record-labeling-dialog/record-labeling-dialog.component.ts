import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  MatDialogRef,
  MAT_DIALOG_DATA,
  MatDialogModule,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';

export type ComplianceLabelType =
  | 'None'
  | 'Export'
  | 'Allergen'
  | 'ForeignLanguage';

export interface RecordLabelingDialogData {
  targetName: string;
}

export interface RecordLabelingDialogResult {
  labelType: ComplianceLabelType;
}

@Component({
  selector: 'app-record-labeling-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title class="dialog-title">
      <mat-icon>label</mat-icon>
      <span>Record Labeling for {{ data.targetName }}</span>
    </h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="labeling-form">
        <mat-form-field appearance="outline">
          <mat-label>Compliance Label Type</mat-label>
          <mat-select formControlName="labelType">
            @for(type of labelTypes; track type.value) {
            <mat-option [value]="type.value">{{ type.viewValue }}</mat-option>
            }
          </mat-select>
          <mat-hint>Select the type of compliance label applied.</mat-hint>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancel</button>
      <button
        mat-flat-button
        color="primary"
        (click)="onConfirm()"
        [disabled]="form.invalid"
      >
        Record Service
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .dialog-title {
        display: flex;
        align-items: center;
        gap: 8px;
      }
      .labeling-form {
        min-width: 350px;
        padding-top: 1rem;
      }
    `,
  ],
})
export class RecordLabelingDialogComponent {
  private fb = inject(FormBuilder);
  public dialogRef = inject(
    MatDialogRef<RecordLabelingDialogComponent, RecordLabelingDialogResult>
  );

  form = this.fb.group({
    labelType: ['', Validators.required],
  });

  labelTypes: { value: ComplianceLabelType; viewValue: string }[] = [
    { value: 'Export', viewValue: 'Export Labeling' },
    { value: 'Allergen', viewValue: 'Allergen Information' },
    { value: 'ForeignLanguage', viewValue: 'Foreign Language Tagging' },
  ];

  constructor(@Inject(MAT_DIALOG_DATA) public data: RecordLabelingDialogData) {}

  onConfirm(): void {
    if (this.form.valid) {
      this.dialogRef.close({
        labelType: this.form.value.labelType as ComplianceLabelType,
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
