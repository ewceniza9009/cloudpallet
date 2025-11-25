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
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

export interface CompleteFumigationDialogData {
  targetName: string;
}

export interface CompleteFumigationDialogResult {
  durationHours: number;
}

@Component({
  selector: 'app-complete-fumigation-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
  ],
  templateUrl: './complete-fumigation-dialog.component.html',
  styleUrls: ['./complete-fumigation-dialog.component.scss']
})
export class CompleteFumigationDialogComponent {
  private fb = inject(FormBuilder);
  public dialogRef = inject(
    MatDialogRef<
      CompleteFumigationDialogComponent,
      CompleteFumigationDialogResult
    >
  );

  form = this.fb.group({
    durationHours: [1, [Validators.required, Validators.min(0.1)]],
  });

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: CompleteFumigationDialogData
  ) {}

  onConfirm(): void {
    if (this.form.valid) {
      this.dialogRef.close({
        durationHours: this.form.value.durationHours as number,
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
