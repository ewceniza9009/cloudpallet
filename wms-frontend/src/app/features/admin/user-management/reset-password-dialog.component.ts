import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-reset-password-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>Reset Password</h2>
    <mat-dialog-content>
      <form [formGroup]="resetForm">
        <p>Enter the new password for this user below.</p>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>New Password</mat-label>
          <input matInput [type]="hidePassword ? 'password' : 'text'" formControlName="newPassword">
          <button mat-icon-button matSuffix (click)="hidePassword = !hidePassword" type="button">
            <mat-icon>{{hidePassword ? 'visibility_off' : 'visibility'}}</mat-icon>
          </button>
          <mat-hint>Minimum 8 characters</mat-hint>
          <mat-error *ngIf="resetForm.get('newPassword')?.hasError('minlength')">
            Password must be at least 8 characters
          </mat-error>
          <mat-error *ngIf="resetForm.get('newPassword')?.hasError('required')">
            Password is required
          </mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" [disabled]="resetForm.invalid" [mat-dialog-close]="resetForm.value.newPassword">
        Reset Password
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width { width: 100%; margin-top: 1rem; }
    mat-dialog-content { min-width: 350px; }
  `]
})
export class ResetPasswordDialogComponent {
  private fb = inject(FormBuilder);
  hidePassword = true;

  resetForm = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(8)]]
  });
}
