import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface AuthUser {
  id: number;
  email: string;
  displayName: string;
}

interface AuthResponse extends AuthUser {
  token: string;
}

const TOKEN_KEY = 'auth-token';
const USER_KEY = 'auth-user';
const API_BASE_URL = 'http://localhost:5269';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly user = signal<AuthUser | null>(readCachedUser());
  readonly isAuthenticated = computed(() => this.user() !== null);

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  async login(email: string, password: string): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${API_BASE_URL}/api/auth/login`, { email, password }),
    );
    this.setSession(res);
  }

  async signup(email: string, password: string, displayName: string): Promise<void> {
    // Standard browser API, no permission prompt — tells us which real-world timezone the
    // driver operates in without asking them a question they'd find odd.
    const timeZoneId = Intl.DateTimeFormat().resolvedOptions().timeZone;
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${API_BASE_URL}/api/auth/signup`, { email, password, displayName, timeZoneId }),
    );
    this.setSession(res);
  }

  async loginAsDemo(): Promise<void> {
    const res = await firstValueFrom(this.http.post<AuthResponse>(`${API_BASE_URL}/api/auth/demo`, {}));
    this.setSession(res);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.user.set(null);
  }

  private setSession(res: AuthResponse): void {
    const user: AuthUser = { id: res.id, email: res.email, displayName: res.displayName };
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.user.set(user);
  }
}

function readCachedUser(): AuthUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}
