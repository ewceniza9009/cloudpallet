import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

export interface ReasonDialogData {
  title: string;
  message: string;
  label: string;
  confirmText: string;
  warn?: boolean;
}

@Component({
  selector: 'app-reason-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule
  ],
  templateUrl: './reason-dialog.component.html',
  styleUrls: ['./reason-dialog.component.scss']
})
export class ReasonDialogComponent {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    public dialogRef: MatDialogRef<ReasonDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ReasonDialogData
  ) {
    this.form = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(3)]]
    });
  }

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.get('reason')?.value);
    }
  }
}
