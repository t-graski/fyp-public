import {Component, inject, computed} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {Router, ActivatedRoute} from '@angular/router';
import {toSignal} from '@angular/core/rxjs-interop';
import {map} from 'rxjs';
import {AppAuthService} from '../../api/auth/app-auth.service';
import {ProfileInfoTabComponent} from './profile-info-tab/profile-info-tab.component';
import {ProfileGradesTabComponent} from './profile-grades-tab/profile-grades-tab.component';
import {UserService} from '../../services/user.service';

type ProfileTab = 'info' | 'grades';
const VALID_TABS: ProfileTab[] = ['info', 'grades'];

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    ProfileInfoTabComponent,
    ProfileGradesTabComponent
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent {
  private readonly authService = inject(AppAuthService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly queryTab = toSignal(
    this.route.queryParamMap.pipe(
      map(p => p.get('tab') as ProfileTab | null)
    )
  );

  $activeTab = computed<ProfileTab>(() => {
    const t = this.queryTab();
    return t && VALID_TABS.includes(t) ? t : 'info';
  });

  isStudent(): boolean {
    return this.userService.getCurrentUser()?.student !== null;
  }

  selectTab(tab: ProfileTab): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {tab},
      replaceUrl: false
    });
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigateByUrl('');
  }
}
