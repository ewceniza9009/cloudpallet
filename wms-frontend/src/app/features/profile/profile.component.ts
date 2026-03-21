import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatSnackBarModule,
    MatDividerModule,
  ],
  template: `
    <div class="profile-container">
      <mat-card class="profile-card">
        <mat-card-header>
          <div mat-card-avatar class="profile-avatar">
            <mat-icon>account_circle</mat-icon>
          </div>
          <mat-card-title>My Profile</mat-card-title>
          <mat-card-subtitle>Update your personal information</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="profileForm" (ngSubmit)="onUpdateProfile()">
            <div class="form-row">
              <mat-form-field appearance="outline">
                <mat-label>First Name</mat-label>
                <input matInput formControlName="firstName" placeholder="First Name">
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Last Name</mat-label>
                <input matInput formControlName="lastName" placeholder="Last Name">
              </mat-form-field>
            </div>

            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Email Address (Read-only)</mat-label>
              <input matInput [value]="authService.currentUser()?.email" readonly>
              <mat-icon matSuffix>lock</mat-icon>
            </mat-form-field>

            <div class="actions">
              <button mat-raised-button color="primary" type="submit" [disabled]="profileForm.invalid || isUpdatingProfile()">
                <mat-icon *ngIf="!isUpdatingProfile()">save</mat-icon>
                <span *ngIf="!isUpdatingProfile()">Update Information</span>
                <span *ngIf="isUpdatingProfile()">Updating...</span>
              </button>
            </div>
          </form>

          <mat-divider class="section-divider"></mat-divider>

          <h3 class="section-title">Security</h3>
          <p class="section-description">Change your account password regularly to stay secure.</p>

          <form [formGroup]="passwordForm" (ngSubmit)="onChangePassword()">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>Current Password</mat-label>
              <input matInput [type]="hideCurrent ? 'password' : 'text'" formControlName="currentPassword">
              <button mat-icon-button matSuffix (click)="hideCurrent = !hideCurrent" type="button">
                <mat-icon>{{hideCurrent ? 'visibility_off' : 'visibility'}}</mat-icon>
              </button>
            </mat-form-field>

            <div class="form-row">
              <mat-form-field appearance="outline">
                <mat-label>New Password</mat-label>
                <input matInput [type]="hideNew ? 'password' : 'text'" formControlName="newPassword">
                <button mat-icon-button matSuffix (click)="hideNew = !hideNew" type="button">
                  <mat-icon>{{hideNew ? 'visibility_off' : 'visibility'}}</mat-icon>
                </button>
                <mat-hint>Minimum 8 characters</mat-hint>
                <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('minlength')">
                  Password must be at least 8 characters
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Confirm New Password</mat-label>
                <input matInput [type]="hideConfirm ? 'password' : 'text'" formControlName="confirmPassword">
                <button mat-icon-button matSuffix (click)="hideConfirm = !hideConfirm" type="button">
                  <mat-icon>{{hideConfirm ? 'visibility_off' : 'visibility'}}</mat-icon>
                </button>
                <mat-error *ngIf="passwordForm.hasError('mismatch')">
                  Passwords do not match
                </mat-error>
              </mat-form-field>
            </div>

            <div class="actions">
              <button mat-stroked-button color="warn" type="submit" [disabled]="passwordForm.invalid || isChangingPassword()">
                <mat-icon *ngIf="!isChangingPassword()">vpn_key</mat-icon>
                <span *ngIf="!isChangingPassword()">Change Password</span>
                <span *ngIf="isChangingPassword()">Changing...</span>
              </button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .profile-container { padding: 2rem; display: flex; justify-content: center; background: var(--mat-sys-background); min-height: calc(100vh - 64px); }
    .profile-card { max-width: 800px; width: 100%; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.05); }
    .profile-avatar { background: var(--mat-sys-primary-container); color: var(--mat-sys-primary); display: flex; align-items: center; justify-content: center; mat-icon { font-size: 40px; width: 40px; height: 40px; }}
    mat-card-content { padding-top: 1.5rem; }
    .form-row { display: flex; gap: 1rem; flex-wrap: wrap; mat-form-field { flex: 1; min-width: 250px; }}
    .full-width { width: 100%; }
    .actions { display: flex; justify-content: flex-end; margin-top: 1rem; }
    .section-divider { margin: 2rem 0; }
    .section-title { font-size: 1.25rem; font-weight: 500; margin-bottom: 0.5rem; color: var(--mat-sys-on-surface); }
    .section-description { color: var(--mat-sys-on-surface-variant); margin-bottom: 1.5rem; font-size: 0.9rem; }
  `]
})
export class ProfileComponent {
  private fb = inject(FormBuilder);
  authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  isUpdatingProfile = signal(false);
  isChangingPassword = signal(false);

  hideCurrent = true;
  hideNew = true;
  hideConfirm = true;

  profileForm = this.fb.group({
    firstName: [this.authService.currentUser()?.firstName || '', [Validators.required]],
    lastName: [this.authService.currentUser()?.lastName || '', [Validators.required]],
  });

  passwordForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  passwordMatchValidator(g: any) {
    return g.get('newPassword').value === g.get('confirmPassword').value
       ? null : {'mismatch': true};
  }

  onUpdateProfile(): void {
    if (this.profileForm.invalid) return;
    this.isUpdatingProfile.set(true);
    
    const command = {
      firstName: this.profileForm.value.firstName!,
      lastName: this.profileForm.value.lastName!
    };

    this.authService.updateProfile(command).subscribe({
      next: () => {
        this.isUpdatingProfile.set(false);
        this.snackBar.open('Profile updated successfully!', 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.isUpdatingProfile.set(false);
        this.snackBar.open('Failed to update profile.', 'Close', { duration: 5000 });
      }
    });
  }

  onChangePassword(): void {
    if (this.passwordForm.invalid) return;
    this.isChangingPassword.set(true);

    const command = {
      currentPassword: this.passwordForm.value.currentPassword!,
      newPassword: this.passwordForm.value.newPassword!
    };

    this.authService.changePassword(command).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.passwordForm.reset();
        this.snackBar.open('Password changed successfully!', 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.isChangingPassword.set(false);
        this.snackBar.open('Failed to change password. Please check your current password.', 'Close', { duration: 5000 });
      }
    });
  }
}
