import {Component, inject, signal, input, output, OnInit} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {AdminCatalogService} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {AdminModuleDto} from '../../../api';
import {AdminCourseDto, CreateModuleDto, UpdateModuleDto} from '../../../api';
import {DynamicTableComponent, TableColumn, TableAction} from '../../dynamic-table/dynamic-table.component';
import {FormsModule} from '@angular/forms';
import {HasPermissionDirective} from '../../../directives/has-permission.directive';

@Component({
  selector: 'app-admin-modules-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule, DynamicTableComponent, FormsModule, HasPermissionDirective],
  templateUrl: './admin-modules-tab.component.html',
  styleUrl: './admin-modules-tab.component.scss'
})
export class AdminModulesTabComponent implements OnInit {
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly snackbarService = inject(SnackbarService);

  $courses = input.required<AdminCourseDto[]>();

  $isLoading = signal(false);
  $modules = signal<AdminModuleDto[]>([]);
  $selectedCourseId = signal<string>('');
  $showModuleDetails = signal<string | null>(null);
  $showCreateModuleModal = signal<boolean>(false);
  $showEditModuleModal = signal<string | null>(null);

  $newModule = signal<CreateModuleDto>({
    moduleCode: '',
    title: '',
    description: '',
    credits: 0,
    level: 0,
    semesterOfStudy: 0,
    term: '',
    runsFrom: '',
    runsTo: '',
    scheduledDay: 1,
    scheduledStartLocal: '',
    scheduledEndLocal: ''
  });

  $editModuleData = signal<UpdateModuleDto>({
    moduleCode: '',
    title: '',
    description: '',
    credits: 0,
    level: 0,
    semesterOfStudy: 0,
    term: '',
    runsFrom: '',
    runsTo: '',
    scheduledDay: 1,
    scheduledStartLocal: '',
    scheduledEndLocal: ''
  });

  private readonly dayLabels = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const;

  public formatScheduledDay(value: number | null | undefined): string {
    if (value == null) return '—';
    const idx = Number(value);
    return Number.isInteger(idx) && idx >= 0 && idx <= 6 ? this.dayLabels[idx] : String(value);
  }

  moduleColumns: TableColumn<AdminModuleDto>[] = [
    {key: 'id', label: 'ID', visible: false, cellClass: 'cell-id'},
    {key: 'moduleCode', label: 'Code', sortable: true, visible: true},
    {key: 'title', label: 'Title', sortable: true, visible: true},
    {key: 'description', label: 'Description', visible: false, cellClass: 'cell-description'},
    {key: 'credits', label: 'Credits', visible: true},
    {key: 'level', label: 'Level', visible: true},
    {key: 'semesterOfStudy', label: 'Semester', visible: true},
    {key: 'term', label: 'Term', visible: true},
    {key: 'runsFrom', label: 'Runs From', visible: false},
    {key: 'runsTo', label: 'Runs To', visible: false},
    {
      key: 'scheduledDay',
      label: 'Scheduled Day',
      visible: false,
      render: (m) => this.formatScheduledDay((m as any).scheduledDay)
    },
    {key: 'scheduledStartLocal', label: 'Start Time', visible: false},
    {key: 'scheduledEndLocal', label: 'End Time', visible: false}
  ];

  moduleActions: TableAction<AdminModuleDto>[] = [
    {
      icon: 'visibility',
      label: 'View Details',
      handler: (module) => this.$showModuleDetails.set(module.id!),
      requiredPermission: 'CatalogRead'
    },
    {
      icon: 'people',
      label: 'View Enrolled Students',
      handler: (module) => this.viewModuleEnrollments(module.id!),
      requiredPermission: 'EnrollmentRead'
    },
    {
      icon: 'edit',
      label: 'Edit Module',
      handler: (module) => this.editModule(module),
      requiredPermission: 'CatalogWrite'
    },
    {
      icon: 'content_copy',
      label: 'Clone Module',
      handler: (module) => this.cloneModule(module),
      requiredPermission: 'CatalogWrite'
    },
    {
      divider: true, icon: '', label: '', handler: () => {
      }
    },
    {
      icon: 'delete',
      label: 'Delete Module',
      danger: true,
      handler: () => this.deleteModule(),
      requiredPermission: 'CatalogDelete'
    }
  ];

  viewEnrollments = output<string>();

  ngOnInit(): void {
    if (this.$courses().length > 0 && !this.$selectedCourseId()) {
      this.$selectedCourseId.set(this.$courses()[0].id || '');
      this.loadModules();
    }
  }

  loadModules(): void {
    this.$isLoading.set(true);
    if (!this.$selectedCourseId()) {
      this.$modules.set([]);
      this.$isLoading.set(false);
      this.snackbarService.show('Please select a course first', 400);
      return;
    }
    this.adminCatalogService.apiAdminCoursesCourseIdModulesGet(this.$selectedCourseId()).subscribe({
      next: (response) => {
        const moduleData = response.data;
        if (Array.isArray(moduleData)) {
          this.$modules.set(moduleData);
        } else {
          this.$modules.set([]);
        }
        this.$isLoading.set(false);
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
        this.$modules.set([]);
        this.$isLoading.set(false);
      }
    });
  }

  reloadModules(): void {
    this.loadModules();
  }

  viewModuleEnrollments(moduleId: string): void {
    this.viewEnrollments.emit(moduleId);
  }

  closeModuleDetails(): void {
    this.$showModuleDetails.set(null);
  }

  editModule(module: AdminModuleDto): void {
    this.$editModuleData.set({
      moduleCode: module.moduleCode || '',
      title: module.title || '',
      description: module.description || '',
      credits: module.credits || 0,
      level: module.level || 0,
      semesterOfStudy: module.semesterOfStudy || 0,
      term: module.term || '',
      runsFrom: module.runsFrom || '',
      runsTo: module.runsTo || '',
      scheduledDay: module.scheduledDay ?? 1,
      scheduledStartLocal: module.scheduledStartLocal || '',
      scheduledEndLocal: module.scheduledEndLocal || ''
    });

    this.$showEditModuleModal.set(module.id!);
  }

  cloneModule(module: AdminModuleDto): void {
    this.$newModule.set({
      moduleCode: `${module.moduleCode}-COPY`,
      title: `${module.title} (Copy)`,
      description: module.description || '',
      credits: module.credits || 0,
      level: module.level || 0,
      semesterOfStudy: module.semesterOfStudy || 0,
      term: module.term || '',
      runsFrom: module.runsFrom || '',
      runsTo: module.runsTo || '',
      scheduledDay: module.scheduledDay ?? 1,
      scheduledStartLocal: module.scheduledStartLocal || '',
      scheduledEndLocal: module.scheduledEndLocal || ''
    });

    this.$showCreateModuleModal.set(true);
  }

  deleteModule(): void {
    if (!confirm('Are you sure you want to delete this module? This action cannot be undone.')) return;

    if (!this.$selectedCourseId()) {
      this.snackbarService.show('Course ID is missing', 400);
      return;
    }

    this.adminCatalogService.apiAdminModulesIdDelete(this.$selectedCourseId())
      .subscribe({
        next: (response: any) => {
          this.snackbarService.showFromApiResponse(response);
          this.loadModules();
        }
      })
  }

  openCreateModuleModal(): void {
    this.$showCreateModuleModal.set(true);
  }

  closeCreateModuleModal(): void {
    this.$showCreateModuleModal.set(false);
  }

  closeEditModuleModal(): void {
    this.$showEditModuleModal.set(null);
  }

  createModule(): void {
    const moduleForm = this.$newModule();
    if (!moduleForm.moduleCode || !moduleForm.title) {
      this.snackbarService.show('Please fill required fields', 400);
      return;
    }

    if (!this.$selectedCourseId()) {
      this.snackbarService.show('Please select a course', 400);
      return;
    }

    this.adminCatalogService.apiAdminCoursesCourseIdModulesPost(
      this.$selectedCourseId(),
      {
        moduleCode: moduleForm.moduleCode,
        title: moduleForm.title,
        description: moduleForm.description,
        credits: moduleForm.credits,
        level: moduleForm.level,
        semesterOfStudy: moduleForm.semesterOfStudy,
        term: moduleForm.term,
        runsFrom: moduleForm.runsFrom,
        runsTo: moduleForm.runsTo,
        scheduledDay: moduleForm.scheduledDay,
        scheduledStartLocal: moduleForm.scheduledStartLocal,
        scheduledEndLocal: moduleForm.scheduledEndLocal
      }
    ).subscribe({
      next: (response) => {
        this.snackbarService.showFromApiResponse(response);
        this.closeCreateModuleModal();
        this.$newModule.set({
          moduleCode: '',
          title: '',
          description: '',
          credits: 0,
          level: 0,
          semesterOfStudy: 0,
          term: '',
          runsFrom: '',
          runsTo: '',
          scheduledDay: 1,
          scheduledStartLocal: '',
          scheduledEndLocal: ''
        });
        this.loadModules();
      },
      error: (err) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }

  updateModule(): void {
    const moduleForm = this.$editModuleData();
    if (!this.$showEditModuleModal()) {
      this.snackbarService.show('Module ID is missing', 400);
      return;
    }

    if (!moduleForm.moduleCode || !moduleForm.title) {
      this.snackbarService.show('Please fill required fields', 400);
      return;
    }

    this.adminCatalogService.apiAdminModulesIdPut(this.$showEditModuleModal()!, {
      moduleCode: moduleForm.moduleCode,
      title: moduleForm.title,
      description: moduleForm.description,
      credits: moduleForm.credits,
      level: moduleForm.level,
      semesterOfStudy: moduleForm.semesterOfStudy,
      term: moduleForm.term,
      runsFrom: moduleForm.runsFrom,
      runsTo: moduleForm.runsTo,
      scheduledDay: moduleForm.scheduledDay,
      scheduledStartLocal: moduleForm.scheduledStartLocal,
      scheduledEndLocal: moduleForm.scheduledEndLocal
    }).subscribe({
      next: (response: any) => {
        this.snackbarService.showFromApiResponse(response);
        this.closeEditModuleModal();
        this.loadModules();
      },
      error: (err: any) => {
        this.snackbarService.showFromApiResponse(err.error);
      }
    });
  }
}
