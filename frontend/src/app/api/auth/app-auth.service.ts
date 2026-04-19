import {Injectable, inject} from '@angular/core';
import {AuthService} from '../api/auth.service';
import {TokenStorageService} from './token-storage.service';
import {Observable, map, tap, switchMap} from 'rxjs';
import {LoginDto} from '../model/loginDto';
import {UserDetailDto} from '../model/userDetailDto';
import {UserService} from '../../services/user.service';


export interface LoginResult {
  accessToken: string;
}

@Injectable({providedIn: 'root'})
export class AppAuthService {
  private readonly apiAuth = inject(AuthService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly userService = inject(UserService);

  login(email: string, password: string, rememberMe: boolean): Observable<UserDetailDto> {
    return this.apiAuth.apiAuthLoginPost({email, password}).pipe(
      map((response) => {
        const token =
          response?.data?.accessToken;

        if (!token) {
          console.error('No token found in response. Response structure:', response);
          throw new Error("Login response did not include a token");
        }

        return token;
      }),
      tap(accessToken => this.tokenStorage.setAccessToken(accessToken, rememberMe)),
      switchMap(() => this.userService.loadCurrentUser())
    );
  }

  logout(): void {
    this.tokenStorage.clear();
  }

  getAccessToken(): string | null {
    return this.tokenStorage.getAccessToken();
  }
}
