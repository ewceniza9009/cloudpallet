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
  templateUrl: './record-labeling-dialog.component.html',
  styleUrls: ['./record-labeling-dialog.component.scss']
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
