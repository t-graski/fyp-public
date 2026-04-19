import {Component, effect, inject, input, output, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {
  BASE_PATH,
  CreateModuleElementDto,
  ModuleElementDto,
  ModuleElementType,
  ModuleFileDto,
  ModuleFilesService,
  ModuleService,
  UpdateModuleElementDto
} from '../../../api';
import {SnackbarService} from '../../../services/snackbar.service';
import {ICON_OPTIONS} from '../../../shared/models/icon-options';

export interface ElementOptions {
  // Headline
  fontSize?: string;
  color?: string;
  bold?: boolean;
  italic?: boolean;
  text?: string;
  // Text
  size?: string;
  // Link
  url?: string;
  name?: string;
  // Assessment
  assessmentName?: string;
}

@Component({
  selector: 'app-module-elements-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule, FormsModule],
  templateUrl: './module-elements-tab.component.html',
  styleUrl: './module-elements-tab.component.scss'
})
export class ModuleElementsTabComponent {
  private readonly moduleService = inject(ModuleService);
  private readonly moduleFilesService = inject(ModuleFilesService);
  private readonly snackbar = inject(SnackbarService);
  private readonly http = inject(HttpClient);
  private readonly basePath = inject(BASE_PATH);

  $elements = input<ModuleElementDto[]>([]);
  $moduleId = input.required<string>();
  $editMode = input(false);
  $highlightedElementId = input<string | null>(null);
  elementsChanged = output<ModuleElementDto[]>();

  constructor() {
    effect(() => {
      const id = this.$highlightedElementId();
      if (!id) return;
      setTimeout(() => {
        const el = document.getElementById('element-' + id);
        el?.scrollIntoView({behavior: 'smooth', block: 'center'});
      }, 50);
    });
  }

  $isSaving = signal(false);
  $dragIndex = signal<number | null>(null);
  $dragOverIndex = signal<number | null>(null);
  $addingType = signal<ModuleElementType | null>(null);

  $newIconKey = signal<string>('');
  $newOptions = signal<ElementOptions>({});
  $newWeight = signal<number | null>(null);

  $editingId = signal<string | null>(null);
  $editIconKey = signal<string>('');
  $editOptions = signal<ElementOptions>({});
  $editWeight = signal<number | null>(null);

  $newUploadedFile = signal<ModuleFileDto | null>(null);
  $isUploading = signal(false);

  $editUploadedFile = signal<ModuleFileDto | null>(null);
  $isEditUploading = signal(false);

  readonly iconOptions = ICON_OPTIONS;

  readonly elementTypes: { type: ModuleElementType; label: string; icon: string }[] = [
    {type: ModuleElementType.NUMBER_1, label: 'Headline', icon: 'title'},
    {type: ModuleElementType.NUMBER_2, label: 'Text', icon: 'notes'},
    {type: ModuleElementType.NUMBER_3, label: 'Link', icon: 'link'},
    {type: ModuleElementType.NUMBER_4, label: 'Assessment', icon: 'assignment'},
    {type: ModuleElementType.NUMBER_5, label: 'File Upload', icon: 'upload_file'},
  ];

  getElementTypeLabel(type: ModuleElementType | undefined): string {
    return this.elementTypes.find(t => t.type === type)?.label ?? 'Element';
  }

  getOptions(element: ModuleElementDto): Record<string, any> {
    return (element.options && typeof element.options === 'object') ? element.options : {};
  }

  selectType(type: ModuleElementType): void {
    this.$addingType.set(type);
    this.$newIconKey.set('');
    this.$newOptions.set({});
    this.$newWeight.set(null);
  }

  patchOptions(patch: Partial<ElementOptions>): void {
    this.$newOptions.update(o => ({...o, ...patch}));
  }

  cancelAdding(): void {
    this.$addingType.set(null);
    this.$newIconKey.set('widgets');
    this.$newOptions.set({});
    this.$newWeight.set(null);
    this.$newUploadedFile.set(null);
  }

  confirmAdd(): void {
    const type = this.$addingType();
    if (type == null) return;

    const opts = this.$newOptions();
    let options: Record<string, any> = {};

    switch (type) {
      case ModuleElementType.NUMBER_1:
        options = {
          text: opts.text ?? '',
          fontSize: opts.fontSize ?? '24',
          color: opts.color ?? '',
          bold: opts.bold ?? false,
          italic: opts.italic ?? false
        };
        break;
      case ModuleElementType.NUMBER_2:
        options = {text: opts.text ?? '', size: opts.size ?? 'md'};
        break;
      case ModuleElementType.NUMBER_3:
        options = {url: opts.url ?? '', name: opts.name ?? '', color: opts.color ?? ''};
        break;
      case ModuleElementType.NUMBER_4:
        options = {name: opts.assessmentName ?? ''};
        break;
      case ModuleElementType.NUMBER_5:
        options = {
          fileId: this.$newUploadedFile()?.id ?? null,
          fileName: this.$newUploadedFile()?.originalFileName ?? null
        };
        break;
    }

    const dto: CreateModuleElementDto = {
      type,
      sortOrder: (this.$elements().length + 1) * 10,
      iconKey: this.$newIconKey(),
      options,
      assessmentWeight: type === ModuleElementType.NUMBER_4 ? (this.$newWeight() ?? null) : null,
    };

    this.$isSaving.set(true);
    this.moduleService.apiModulesModuleIdElementsPost(this.$moduleId(), dto).subscribe({
      next: (res) => {
        this.$isSaving.set(false);
        this.cancelAdding();
        if (res.data) {
          this.elementsChanged.emit([...this.$elements(), res.data]);
        }
        this.snackbar.show('Element added', 200);
      },
      error: () => {
        this.$isSaving.set(false);
        this.snackbar.show('Failed to add element', 500);
      }
    });
  }

  deleteElement(element: ModuleElementDto): void {
    if (!element.id) return;
    this.$isSaving.set(true);
    this.moduleService.apiModulesModuleIdElementsElementIdDelete(this.$moduleId(), element.id).subscribe({
      next: () => {
        this.$isSaving.set(false);
        this.elementsChanged.emit(this.$elements().filter(e => e.id !== element.id));
        this.snackbar.show('Element deleted', 200);
      },
      error: () => {
        this.$isSaving.set(false);
        this.snackbar.show('Failed to delete element', 500);
      }
    });
  }

  onDragStart(index: number): void {
    this.$dragIndex.set(index);
  }

  onDragOver(event: DragEvent, index: number): void {
    event.preventDefault();
    this.$dragOverIndex.set(index);
  }

  onDrop(event: DragEvent, dropIndex: number): void {
    event.preventDefault();

    const dragIndex = this.$dragIndex();

    if (dragIndex == null || dragIndex === dropIndex) {
      this.$dragIndex.set(null);
      this.$dragOverIndex.set(null);
      return;
    }

    const reordered = [...this.$elements()];
    const [moved] = reordered.splice(dragIndex, 1);

    reordered.splice(dropIndex, 0, moved);

    this.elementsChanged.emit(reordered);
    this.$dragIndex.set(null);
    this.$dragOverIndex.set(null);
    this.$isSaving.set(true);

    this.moduleService.apiModulesModuleIdElementsReorderPut(this.$moduleId(), {
      elementIdsInOrder: reordered.map(e => e.id!)
    }).subscribe({
      next: () => {
        this.$isSaving.set(false);
      },
      error: () => {
        this.$isSaving.set(false);
        this.snackbar.show('Failed to save order', 500);
      }
    });
  }

  onDragEnd(): void {
    this.$dragIndex.set(null);
    this.$dragOverIndex.set(null);
  }

  startEditing(element: ModuleElementDto): void {
    this.$editingId.set(element.id ?? null);
    this.$editIconKey.set(element.iconKey ?? '');
    this.$editUploadedFile.set(null);

    const opts = this.getOptions(element);

    this.$editOptions.set({
      text: opts['text'] ?? '',
      fontSize: opts['fontSize'] ?? '24',
      color: opts['color'] ?? '',
      bold: opts['bold'] ?? false,
      italic: opts['italic'] ?? false,
      size: opts['size'] ?? 'md',
      url: opts['url'] ?? '',
      name: opts['name'] ?? '',
      assessmentName: opts['name'] ?? '',
    });

    this.$editWeight.set(element.assessmentWeight ?? null);
  }

  cancelEditing(): void {
    this.$editingId.set(null);
    this.$editUploadedFile.set(null);
  }

  patchEditOptions(patch: Partial<ElementOptions>): void {
    this.$editOptions.update(o => ({...o, ...patch}));
  }

  confirmEdit(element: ModuleElementDto): void {
    if (!element.id) return;
    const opts = this.$editOptions();
    const existingOpts = this.getOptions(element);
    let options: Record<string, any> = {};

    switch (element.type) {
      case ModuleElementType.NUMBER_1:
        options = {
          text: opts.text ?? '',
          fontSize: opts.fontSize ?? '24',
          color: opts.color ?? '',
          bold: opts.bold ?? false,
          italic: opts.italic ?? false
        };
        break;
      case ModuleElementType.NUMBER_2:
        options = {text: opts.text ?? '', size: opts.size ?? 'md'};
        break;
      case ModuleElementType.NUMBER_3:
        options = {url: opts.url ?? '', name: opts.name ?? '', color: opts.color ?? ''};
        break;
      case ModuleElementType.NUMBER_4:
        options = {name: opts.assessmentName ?? ''};
        break;
      case ModuleElementType.NUMBER_5:
        options = {
          fileId: this.$editUploadedFile()?.id ?? existingOpts['fileId'] ?? null,
          fileName: this.$editUploadedFile()?.originalFileName ?? existingOpts['fileName'] ?? null
        };
        break;
    }

    const dto: UpdateModuleElementDto = {
      iconKey: this.$editIconKey(),
      options,
      assessmentWeight: element.type === ModuleElementType.NUMBER_4 ? (this.$editWeight() ?? null) : null,
    };

    this.$isSaving.set(true);
    this.moduleService.apiModulesModuleIdElementsElementIdPut(this.$moduleId(), element.id, dto).subscribe({
      next: (res) => {
        this.$isSaving.set(false);
        this.$editingId.set(null);
        if (res.data) {
          this.elementsChanged.emit(this.$elements().map(e => e.id === element.id ? res.data! : e));
        }
        this.snackbar.show('Element updated', 200);
      },
      error: () => {
        this.$isSaving.set(false);
        this.snackbar.show('Failed to update element', 500);
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) return;

    if (file.type !== 'application/pdf') {
      this.snackbar.show('Only PDF files are allowed', 400);
      input.value = '';
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      this.snackbar.show('File must be 50 MB or smaller', 400);
      input.value = '';
      return;
    }

    this.$isUploading.set(true);
    this.moduleFilesService.apiModulesModuleIdFilesPost(this.$moduleId(), file).subscribe({
      next: (res) => {
        this.$isUploading.set(false);

        if (res.data) {
          this.$newUploadedFile.set(res.data);
        }

        this.snackbar.show('File uploaded', 200);
        input.value = '';
      },
      error: () => {
        this.$isUploading.set(false);
        this.snackbar.show('Failed to upload file', 500);
        input.value = '';
      }
    });
  }

  onEditFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) return;

    if (file.type !== 'application/pdf') {
      this.snackbar.show('Only PDF files are allowed', 400);
      input.value = '';
      return;
    }

    if (file.size > 50 * 1024 * 1024) {
      this.snackbar.show('File must be 50 MB or smaller', 400);
      input.value = '';
      return;
    }

    this.$isEditUploading.set(true);
    this.moduleFilesService.apiModulesModuleIdFilesPost(this.$moduleId(), file).subscribe({
      next: (res) => {
        this.$isEditUploading.set(false);

        if (res.data) {
          this.$editUploadedFile.set(res.data);
        }

        this.snackbar.show('File uploaded', 200);
        input.value = '';
      },
      error: () => {
        this.$isEditUploading.set(false);
        this.snackbar.show('Failed to upload file', 500);
        input.value = '';
      }
    });
  }

  downloadFile(element: ModuleElementDto): void {
    const opts = this.getOptions(element);
    const fileId = opts['fileId'] as string | null;
    const fileName = opts['fileName'] as string | null;

    if (!fileId) return;

    const url = `${this.basePath}/api/modules/${this.$moduleId()}/files/${fileId}/download`;

    this.http.get(url, {responseType: 'blob'}).subscribe({
      next: (blob: Blob) => {
        const objectUrl = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = objectUrl;
        a.download = fileName ?? 'download';
        a.click();
        URL.revokeObjectURL(objectUrl);
      },
      error: () => this.snackbar.show('Failed to download file', 500)
    });
  }
}
