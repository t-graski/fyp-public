import {Component, inject, OnInit, signal} from '@angular/core';
import {NavigationEnd, Router, RouterOutlet} from '@angular/router';
import {SnackbarComponent} from './components/snackbar/snackbar.component';
import {Navbar} from './components/navbar/navbar';
import {UserService} from './services/user.service';
import {filter} from 'rxjs/operators';
import {CommonModule} from '@angular/common';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  imports: [
    CommonModule,
    RouterOutlet,
    SnackbarComponent,
    Navbar
  ],
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private readonly router = inject(Router);
  private readonly userService = inject(UserService);

  $showNavbar = signal(false);

  ngOnInit(): void {
    this.updateNavbarVisibility();

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updateNavbarVisibility();
    });
  }

  private updateNavbarVisibility(): void {
    const currentUrl = this.router.url;
    const isLoginPage = currentUrl === '/'
      || currentUrl === ''
      || currentUrl.includes('login');
    const hasUser = !!this.userService.getCurrentUser();

    this.$showNavbar.set(!isLoginPage && hasUser);
  }

  getUserName(): string {
    const user = this.userService.getCurrentUser();

    if (user?.firstName && user?.lastName) {
      return `${user.firstName} ${user.lastName}`;
    } else if (user?.email) {
      const emailName = user.email.split('@')[0];
      return emailName
        .split('.')
        .map(part => part.charAt(0).toUpperCase() + part.slice(1))
        .join(' ');
    }

    return '';
  }

  getUserEmail(): string {
    const user = this.userService.getCurrentUser();
    return user?.email || '';
  }

  getTitle(): string {
    const currentUrl = this.router.url;
    if (currentUrl.includes('/admin')) return 'Admin Dashboard';
    if (currentUrl.includes('/staff')) return 'Staff Dashboard';
    if (currentUrl.includes('/attendance')) return 'Attendance Management';
    if (currentUrl.includes('/profile')) return 'Profile';
    return 'Student Dashboard';
  }

  getSubTitle(): string {
    const userName = this.getUserName();
    return userName ? `Welcome back, ${userName}` : '';
  }
}
