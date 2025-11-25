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

export interface StartQuarantineDialogData {
  targetName: string;
}

export interface StartQuarantineDialogResult {
  reason: string;
}

@Component({
  selector: 'app-start-quarantine-dialog',
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
  templateUrl: './start-quarantine-dialog.component.html',
  styleUrls: ['./start-quarantine-dialog.component.scss']
})
export class StartQuarantineDialogComponent {
  private fb = inject(FormBuilder);
  public dialogRef = inject(
    MatDialogRef<StartQuarantineDialogComponent, StartQuarantineDialogResult>
  );

  form = this.fb.group({
    reason: ['', Validators.required],
  });

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: StartQuarantineDialogData
  ) {}

  onConfirm(): void {
    if (this.form.valid) {
      this.dialogRef.close({ reason: this.form.value.reason as string });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
