import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DecodedToken {
  role: 'Admin' | 'Operator' | 'Finance';
  email: string;
  sub: string;
  [key: string]: any;
}

export interface RegisterRequest {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    role: 'Admin' | 'Operator' | 'Finance';
}

export interface LoginRequest {
    email: string;
    password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private readonly TOKEN_KEY = 'wms_auth_token';
  private apiUrl = environment.apiUrl;

  currentUser = signal<DecodedToken | null>(null);

  isLoggedIn = computed(() => !!this.currentUser());
  currentUserRole = computed(() => this.currentUser()?.role ?? null);

  constructor() {
    const token = this.getToken();
    if (token) {
      this.currentUser.set(this._decodeToken(token));
    }
  }

  register(request: RegisterRequest) {
    return this.http.post(`${this.apiUrl}/Authentication/register`, request);
  }

  login(email: string, password: string) {
    const request: LoginRequest = { email, password };
    return this.http.post<{ token: string }>(`${this.apiUrl}/Authentication/login`, request).pipe(
      tap(response => {
        this.setToken(response.token);
        this.currentUser.set(this._decodeToken(response.token));
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUser.set(null);
  }

  hasRole(role: 'Admin' | 'Operator' | 'Finance'): boolean {
    const userRole = this.currentUserRole();
    if (userRole === 'Admin') {
      return true;
    }
    return userRole === role;
  }

  public getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  private _decodeToken(token: string): DecodedToken | null {
    if (!token) return null;
    try {
      const payload = token.split('.')[1];
      return JSON.parse(atob(payload));
    } catch (e) {
      console.error("Failed to decode token", e);
      return null;
    }
  }
}
