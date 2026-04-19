import {Component, computed, inject, input, signal} from '@angular/core';
import {MatIconModule} from '@angular/material/icon';
import {AppAuthService} from '../../api/auth/app-auth.service';
import {Router} from '@angular/router';
import {CommonModule} from '@angular/common';
import {NavigationService} from '../../services/navigation.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
  imports: [
    CommonModule,
    MatIconModule,
  ]
})
export class Navbar {
  private readonly authService = inject(AppAuthService);
  private readonly router = inject(Router);
  private readonly navigationService = inject(NavigationService);

  $title = input<string>('');
  $subTitle = input<string>('');
  $userName = input<string>('');
  $userEmail = input<string>('');

  $showUserDropdown = signal(false);

  $links = computed(() => this.navigationService.getNavigationLinks());

  navigateTo(path: string): void {
    void this.router.navigateByUrl(path);
  }

  navigateToDashboard(): void {
    this.navigationService.navigateToHomeDashboard();
  }

  toggleUserDropdown(event: Event): void {
    event.stopPropagation();
    this.$showUserDropdown.update(v => !v);
  }

  navigateToProfile(): void {
    this.$showUserDropdown.set(false);
    void this.router.navigateByUrl('/profile');
  }

  logout(): void {
    this.$showUserDropdown.set(false);
    this.authService.logout();
    void this.router.navigateByUrl('');
  }
}
