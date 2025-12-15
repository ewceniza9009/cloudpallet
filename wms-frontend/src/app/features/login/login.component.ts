import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatSnackBarModule,
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  loginForm = this.fb.group({
    email: ['operator@wms.com', [Validators.required, Validators.email]],
    password: ['Pa$$w0rd', [Validators.required]],
  });

  loginAs(role: 'Admin' | 'Operator' | 'Finance'): void {
    const emailMap = {
      Admin: 'admin@wms.com',
      Operator: 'operator@wms.com',
      Finance: 'finance@wms.com',
    };
    this.loginForm.patchValue({
      email: emailMap[role],
      password: 'Pa$$w0rd',
    });
    this.onSubmit();
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }
    const { email, password } = this.loginForm.value;
    this.authService.login(email!, password!).subscribe({
      next: () => {
        const role = this.authService.currentUserRole();
        if (role === 'Operator') {
          this.router.navigate(['/mobile']);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err) => {
        this.snackBar.open('Login failed: Invalid credentials.', 'Close', {
          duration: 5000,
        });
      },
    });
  }
}
