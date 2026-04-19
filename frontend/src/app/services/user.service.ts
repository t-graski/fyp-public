import {inject, Injectable, signal} from '@angular/core';
import {map, Observable, tap} from 'rxjs';
import {UserDetailDto} from '../api';
import {UserService as ApiUserService} from '../api/';

@Injectable({providedIn: 'root'})
export class UserService {
  private readonly apiUserService = inject(ApiUserService);
  private readonly $currentUser = signal<UserDetailDto | null>(null);

  getCurrentUser(): UserDetailDto | null {
    return this.$currentUser();
  }

  loadCurrentUser(): Observable<UserDetailDto> {
    return this.apiUserService.apiUsersMeGet().pipe(
      map(response => {
        if (!response.data) {
          throw new Error('No user data in response');
        }
        return response.data;
      }),
      tap(user => this.$currentUser.set(user))
    )
  }
}
