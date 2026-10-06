import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenService {
  private _token: string | null = null;

  getToken(): string | null {
    return this._token;
  }

  setToken(token: string | null): void {
    this._token = token;
  }
}
