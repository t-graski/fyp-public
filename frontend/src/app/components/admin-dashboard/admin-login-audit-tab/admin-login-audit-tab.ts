import {Component, inject, signal, computed, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {FormsModule} from '@angular/forms';
import {AdminAuditService, LoginEventDto} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';

@Component({
  selector: 'app-admin-login-audit-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule, FormsModule],
  templateUrl: './admin-login-audit-tab.html',
  styleUrl: './admin-login-audit-tab.scss',
})
export class AdminLoginAuditTab implements OnInit {
  private readonly adminAuditService = inject(AdminAuditService);
  private readonly snackbarService = inject(SnackbarService);

  readonly pageSizeOptions = [10, 25, 50, 100];

  $isLoading = signal(false);
  $loginEvents = signal<LoginEventDto[]>([]);
  $showLoginEventDetails = signal<string | null>(null);
  $currentPage = signal(0);
  $hasNextPage = signal(false);
  $pageSize = signal(50);

  $displayedCount = computed(() => this.$loginEvents().length);

  $loginFilters = signal<LoginFilterOptions>(<LoginFilterOptions>{});

  ngOnInit(): void {
    this.loadLoginEvents();
  }

  loadLoginEvents(): void {
    this.$isLoading.set(true);
    const filters = this.$loginFilters();
    const pageSize = this.$pageSize();
    const offset = this.$currentPage() * pageSize;

    this.adminAuditService.apiAdminAuditLoginsGet(
      filters.actorUserId || undefined,
      pageSize + 1,
      offset
    ).subscribe({
      next: (response) => {
        const loginData = response.data;
        if (Array.isArray(loginData)) {
          this.$hasNextPage.set(loginData.length > pageSize);
          this.$loginEvents.set(loginData.slice(0, pageSize));
        } else {
          this.$loginEvents.set([]);
          this.$hasNextPage.set(false);
        }
        this.$isLoading.set(false);
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
        this.$isLoading.set(false);
      }
    });
  }

  updateLoginFilter(key: keyof LoginFilterOptions, value: string): void {
    this.$loginFilters.update(filters => ({...filters, [key]: value}));
  }

  applyLoginFilters(): void {
    this.$currentPage.set(0);
    this.loadLoginEvents();
  }

  clearLoginFilters(): void {
    this.$loginFilters.set(<LoginFilterOptions>{});
    this.$currentPage.set(0);
    this.loadLoginEvents();
  }

  goToPreviousPage(): void {
    if (this.$currentPage() > 0) {
      this.$currentPage.update(p => p - 1);
      this.loadLoginEvents();
    }
  }

  goToNextPage(): void {
    if (this.$hasNextPage()) {
      this.$currentPage.update(p => p + 1);
      this.loadLoginEvents();
    }
  }

  changePageSize(size: number): void {
    this.$pageSize.set(size);
    this.$currentPage.set(0);
    this.loadLoginEvents();
  }

  scrollToTop(): void {
    window.scrollTo({top: 0, behavior: 'smooth'});
  }

  viewLoginEventDetails(eventId: string): void {
    this.$showLoginEventDetails.set(eventId);
  }

  closeLoginEventDetails(): void {
    this.$showLoginEventDetails.set(null);
  }

  formatLoginDate(dateString: string | null | undefined): string {
    if (!dateString) return 'N/A';
    try {
      const date = new Date(dateString);
      return date.toLocaleString();
    } catch {
      return dateString;
    }
  }
}

type LoginFilterOptions = {
  actorUserId: string,
}
