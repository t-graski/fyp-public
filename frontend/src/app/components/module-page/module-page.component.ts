import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {ActivatedRoute, Router} from '@angular/router';
import {ModuleRagService, ModuleService} from '../../api';
import {ModuleDto} from '../../api';
import {ModuleElementsTabComponent} from './module-elements-tab/module-elements-tab.component';
import {ModuleMembersTabComponent} from './module-members-tab/module-members-tab.component';
import {ModuleGradesTabComponent} from './module-grades-tab/module-grades-tab.component';
import {ModuleInfoTabComponent} from './module-info-tab/module-info-tab.component';
import {ModuleAiChatComponent} from './module-ai-chat/module-ai-chat.component';
import {UserService} from '../../services/user.service';
import {NavigationService} from '../../services/navigation.service';
import {SnackbarService} from '../../services/snackbar.service';

type ModuleTab = 'elements' | 'members' | 'grades' | 'info';

@Component({
  selector: 'app-module-page',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    ModuleElementsTabComponent,
    ModuleMembersTabComponent,
    ModuleGradesTabComponent,
    ModuleInfoTabComponent,
    ModuleAiChatComponent
  ],
  templateUrl: './module-page.component.html',
  styleUrl: './module-page.component.scss'
})
export class ModulePageComponent implements OnInit {
  private readonly moduleService = inject(ModuleService);
  private readonly moduleRagService = inject(ModuleRagService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(UserService);
  private readonly snackbarService = inject(SnackbarService);
  private readonly navigationService = inject(NavigationService);

  $module = signal<ModuleDto | null>(null);
  $isLoading = signal(true);
  $error = signal<string | null>(null);
  $activeTab = signal<ModuleTab>('elements');
  $editMode = signal(false);
  $isChatOpen = signal(false);
  $highlightedElementId = signal<string | null>(null);
  $isIndexing = signal(false);

  onHighlightElement(elementId: string): void {
    this.$highlightedElementId.set(null);
    this.$activeTab.set('elements');
    setTimeout(() => this.$highlightedElementId.set(elementId));
  }

  $isCurrentUserStaff = computed(() => {
    const module = this.$module();
    const currentUserId = this.userService.getCurrentUser()?.id;
    if (!module?.staff || !currentUserId) return false;
    return module.staff.some(s => s.userId === currentUserId);
  });

  toggleEditMode(): void {
    this.$editMode.update(v => !v);
  }

  onElementsChanged(elements: ModuleDto['elements']): void {
    this.$module.update(m => m ? {...m, elements} : m);
  }

  ngOnInit(): void {
    const moduleId = this.route.snapshot.paramMap.get('id');
    if (moduleId) {
      this.loadModule(moduleId);
    }
  }

  loadModule(id: string): void {
    this.$isLoading.set(true);
    this.$error.set(null);
    this.moduleService.apiModulesIdGet(id).subscribe({
      next: (response) => {
        this.$module.set(response.data ?? null);
        this.$isLoading.set(false);
      },
      error: () => {
        this.$error.set('Failed to load module. Please try again.');
        this.$isLoading.set(false);
      }
    });
  }

  setTab(tab: ModuleTab): void {
    this.$activeTab.set(tab);
  }

  goBack(): void {
    this.navigationService.navigateToHomeDashboard();
  }

  navigateToGrading(): void {
    this.router.navigate(['/modules', this.$module()?.id, 'grading']).then(r => r);
  }

  indexModuleContent(): void {
    const moduleId = this.$module()?.id;
    if (!moduleId || this.$isIndexing()) return;

    this.$isIndexing.set(true);
    this.moduleRagService.apiModulesModuleIdRagIndexPost(moduleId, { rebuild: true }).subscribe({
      next: (res) => {
        this.$isIndexing.set(false);
        this.snackbarService.showFromApiResponse(res);
      },
      error: (res) => {
        this.$isIndexing.set(false)
        this.snackbarService.showFromApiResponse(res);
      }
    });
  }
}
