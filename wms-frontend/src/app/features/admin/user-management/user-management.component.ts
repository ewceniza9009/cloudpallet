import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AdminApiService, UserDto, UserRole } from '../admin-api.service';
import { ResetPasswordDialogComponent } from './reset-password-dialog.component';

interface RoleOption {
  value: UserRole;
  label: string;
  icon: string;
  description: string;
}

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatSelectModule,
    MatFormFieldModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDialogModule,
  ],
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss'],
})
export class UserManagementComponent implements OnInit {
  private adminApi = inject(AdminApiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  users = signal<UserDto[]>([]);
  isLoading = signal(true);

  roleOptions: RoleOption[] = [
    {
      value: 'Admin',
      label: 'Administrator',
      icon: 'admin_panel_settings',
      description: 'Full access',
    },
    {
      value: 'Operator',
      label: 'Operator',
      icon: 'engineering',
      description: 'Warehouse operator',
    },
    {
      value: 'Finance',
      label: 'Finance',
      icon: 'account_balance',
      description: 'Financial access',
    },
  ];

  displayedColumns = ['username', 'name', 'email', 'role', 'status', 'actions'];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.adminApi.getUsers().subscribe({
      next: (data) => {
        this.users.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to load users.', 'Close', {
          duration: 5000,
        });
        this.isLoading.set(false);
      },
    });
  }

  onRoleChange(userId: string, newRole: UserRole): void {
    this.adminApi.updateUserRole(userId, newRole).subscribe({
      next: () => {
        this.snackBar.open('User role updated successfully!', 'OK', {
          duration: 3000,
        });
        this.loadUsers();
      },
      error: () =>
        this.snackBar.open('Failed to update user role.', 'Close', {
          duration: 5000,
        }),
    });
  }

  toggleUserStatus(user: UserDto): void {
    const newStatus = !user.isActive;
    this.adminApi.updateUserStatus(user.id, newStatus).subscribe({
      next: () => {
        this.snackBar.open(`User ${newStatus ? 'unblocked' : 'blocked'} successfully!`, 'OK', { duration: 3000 });
        this.loadUsers();
      },
      error: () => this.snackBar.open('Failed to update user status.', 'Close', { duration: 5000 })
    });
  }

  resetPassword(userId: string): void {
    const dialogRef = this.dialog.open(ResetPasswordDialogComponent, { width: '400px' });

    dialogRef.afterClosed().subscribe(newPassword => {
      if (!newPassword) return;

      this.adminApi.resetUserPassword(userId, newPassword).subscribe({
        next: () => this.snackBar.open('Password reset successfully!', 'OK', { duration: 3000 }),
        error: () => this.snackBar.open('Failed to reset password.', 'Close', { duration: 5000 })
      });
    });
  }

  getRoleOption(role: UserRole): RoleOption {
    return (
      this.roleOptions.find((option) => option.value === role) ||
      this.roleOptions[0]
    );
  }
}
