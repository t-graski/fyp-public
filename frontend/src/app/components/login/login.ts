import {ChangeDetectionStrategy, ChangeDetectorRef, Component, inject} from '@angular/core';
import {MatIconModule} from '@angular/material/icon';
import {AppAuthService} from '../../api/auth/app-auth.service';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {finalize, switchMap} from 'rxjs/operators'
import {forkJoin} from 'rxjs';
import {UserService} from '../../services/user.service';
import {NavigationService} from '../../services/navigation.service';
import {PermissionService} from '../../services/permission.service';
import {RoleService} from '../../services/role.service';
import {CommonModule} from '@angular/common';

@Component({
  selector: 'app-login',
  imports: [CommonModule, MatIconModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AppAuthService);
  private readonly userService = inject(UserService);
  private readonly permissionService = inject(PermissionService);
  private readonly roleService = inject(RoleService);
  private readonly navigationService = inject(NavigationService);
  private readonly cdr = inject(ChangeDetectorRef);

  isSubmitting = false;
  errorMessage: string | null = null;
  showHelpText = false;

  readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [true]
  });

  toggleHelpText(): void {
    this.showHelpText = !this.showHelpText;
    this.cdr.markForCheck();
  }

  signIn(): void {
    if (this.loginForm.invalid || this.isSubmitting) return;

    this.isSubmitting = true;
    this.errorMessage = null;
    this.cdr.markForCheck();

    const {email, password, rememberMe} = this.loginForm.getRawValue();

    this.auth
      .login(email, password, rememberMe)
      .pipe(
        switchMap(() => this.userService.loadCurrentUser()),
        switchMap(() => forkJoin([
          this.permissionService.loadPermissions(),
          this.roleService.loadRoles()
        ])),
        finalize(() => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          this.navigationService.navigateToHomeDashboard();
        },
        error: (err) => {
          if (err?.status === 401) this.errorMessage = 'Invalid email or password.';
          else if (err?.status === 403) this.errorMessage = 'Account deactivated. Contact your administrator.';
          else if (err?.status === 429) this.errorMessage = 'Too many failed attempts. Please try again later.'
          else this.errorMessage = 'Login failed. Please try again.';
          this.cdr.markForCheck();
        }
      })
  }
}
