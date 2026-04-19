import {Injectable, inject, signal, computed} from '@angular/core';
import {Observable} from 'rxjs';
import {RoleService as ApiRoleService} from '../api/api/role.service';
import {RoleDto} from '../api';

@Injectable({providedIn: 'root'})
export class RoleService {
  private readonly apiRoleService = inject(ApiRoleService);
  private readonly roles = signal<RoleDto[]>([]);

  public readonly $rolesLoaded = computed(() => this.roles().length > 0);
  public readonly $roles = computed(() => this.roles());

  constructor() {
  }

  loadRoles(): Observable<void> {
    return new Observable<void>(observer => {
      this.apiRoleService.apiRolesGet().subscribe({
        next: (response) => {
          if (response.data) {
            this.roles.set(response.data);
          }
          observer.next();
          observer.complete();
        },
        error: (error) => {
          console.error('Failed to load roles:', error);
          observer.next();
          observer.complete();
        }
      });
    });
  }
}
