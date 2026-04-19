import {inject, Injectable} from '@angular/core';
import {Router} from '@angular/router';
import {PermissionService} from './permission.service';
import {NavLink} from '../shared/models/nav-link.model';

@Injectable({providedIn: 'root'})
export class NavigationService {
  private readonly permissionService = inject(PermissionService);
  private readonly router = inject(Router);


  navigateToHomeDashboard(): void {
    if (this.permissionService.$canAccessAdminDashboard()) {
      void this.router.navigateByUrl('/admin');
    } else if (this.permissionService.$canAccessStaffDashboard()) {
      void this.router.navigateByUrl('/staff');
    } else {
      void this.router.navigateByUrl('/dashboard');
    }
  }

  getNavigationLinks(): NavLink[] {
    if (this.permissionService.$canAccessAdminDashboard()) {
      return [
        {label: 'Admin Settings', path: '/admin', icon: 'admin_panel_settings'}
      ];
    } else if (this.permissionService.$canAccessStaffDashboard()) {
      return [
        {label: 'Dashboard', path: '/staff', icon: 'home'},
        {label: 'Attendance', path: '/attendance', icon: 'how_to_reg'}
      ];
    } else {
      return [
        {label: 'Dashboard', path: '/dashboard', icon: 'home'},
        {label: 'Attendance', path: '/attendance', icon: 'how_to_reg'}
      ];
    }
  }
}
