import {Component, signal, computed, input, model, output, inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {FormsModule} from '@angular/forms';
import {PermissionService} from '../../services/permission.service';
import {ContextMenuComponent, ContextMenuAction} from '../context-menu/context-menu.component';

export interface TableColumn<T = any> {
  key: keyof T | string;
  label: string;
  sortable?: boolean;
  visible?: boolean;
  render?: (item: T) => string;
  value?: (item: T) => any;
  cellClass?: string;
}

export interface TableAction<T = any> {
  icon?: string;
  label: string;
  handler: (item: T) => void;
  danger?: boolean;
  divider?: boolean;
  requiredPermission?: string | number | (string | number)[];
  hidden?: boolean;
  disabled?: boolean | ((item: T) => boolean);
}

@Component({
  selector: 'app-dynamic-table',
  standalone: true,
  imports: [CommonModule, MatIconModule, FormsModule, ContextMenuComponent],
  templateUrl: './dynamic-table.component.html',
  styleUrl: './dynamic-table.component.scss'
})
export class DynamicTableComponent<T extends Record<string, any>> {
  $data = input<T[]>([]);
  $columns = model<TableColumn<T>[]>([]);

  $actions = input<TableAction<T>[]>([]);
  $searchPlaceholder = input<string>("Search...");
  $emptyMessage = input<string>("No data found");
  $loading = input<boolean>(false);

  $contextMenuAction = output<{ action: TableAction<T>, item: T }>();
  $rowClick = output<T>();
  $reload = output<void>();

  $searchQuery = signal('');
  $sortField = signal<keyof T | null>(null);
  $sortDirection = signal<'asc' | 'desc'>('asc');
  $showColumnSelector = signal(false);

  $contextMenuPosition = signal<{ x: number, y: number }>({x: 0, y: 0});
  $isContextMenuOpen = signal(false);
  $contextMenuItem = signal<T | null>(null);

  $showDeleteConfirmation = signal(false);
  $deleteConfirmationMessage = signal('');
  pendingDeleteAction: (() => void) | null = null;

  $contextMenuActions = computed<ContextMenuAction[]>(() => {
    const item = this.$contextMenuItem();
    return this.$actions().map(action => ({
      label: action.label,
      icon: action.icon,
      danger: action.danger,
      divider: action.divider,
      requiredPermission: action.requiredPermission,
      hidden: action.hidden,
      disabled: typeof action.disabled === 'function'
        ? (item ? action.disabled(item) : false)
        : action.disabled,
      action: () => this.handleAction(action)
    }));
  });

  $visibleColumns = computed(() =>
    this.$columns().filter(col => col.visible !== false)
  );

  $filteredData = computed(() => {
    let filtered = this.$data();
    const query = this.$searchQuery().toLowerCase();

    if (query) {
      filtered = filtered.filter(item => {
        return this.$visibleColumns().some(col => {
          const value = this.getColumnValue(item, col);
          if (value == null) return false;
          return String(value).toLowerCase().includes(query);
        });
      });
    }

    return this.sortData(filtered);
  });

  sortData(data: T[]): T[] {
    const field = this.$sortField();
    if (!field) return data;

    const direction = this.$sortDirection();
    const sorted = [...data];
    const column = this.$columns().find(c => c.key === field);

    if (!column) return sorted;

    sorted.sort((a, b) => {
      const aVal = this.getColumnValue(a, column);
      const bVal = this.getColumnValue(b, column);

      if (aVal == null && bVal == null) return 0;
      if (aVal == null) return 1;
      if (bVal == null) return -1;

      const result = String(aVal).localeCompare(String(bVal), undefined, {
        numeric: true,
        sensitivity: 'base'
      });

      return direction === 'asc' ? result : -result;
    });

    return sorted;
  }

  toggleSort(column: TableColumn<T>): void {
    if (!column.sortable) return;

    if (this.$sortField() === column.key) {
      this.$sortDirection.update(dir => dir === 'asc' ? 'desc' : 'asc');
    } else {
      this.$sortField.set(column.key);
      this.$sortDirection.set('asc');
    }
  }

  getSortIcon(column: TableColumn<T>): string {
    if (!column.sortable) return '';
    if (this.$sortField() !== column.key) return 'unfold_more';
    return this.$sortDirection() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  toggleColumn(column: TableColumn<T>): void {
    column.visible = !column.visible;
    this.$columns.set([...this.$columns()]);
  }

  applyColumnChanges(): void {
    this.closeColumnSelector();
  }

  toggleColumnSelector(): void {
    this.$showColumnSelector.update(v => !v);
  }

  closeColumnSelector(): void {
    this.$showColumnSelector.set(false);
  }

  getCellValue(item: T, column: TableColumn<T>): string {
    if (column.render) {
      return column.render(item);
    }
    const value = item[column.key];
    return value != null ? String(value) : '-';
  }

  openContextMenu(event: MouseEvent, item: T): void {
    event.preventDefault();
    event.stopPropagation();
    this.$contextMenuItem.set(item);
    this.$contextMenuPosition.set({x: event.clientX, y: event.clientY});
    this.$isContextMenuOpen.set(true);
  }

  closeContextMenu(): void {
    this.$isContextMenuOpen.set(false);
    this.$contextMenuItem.set(null);
  }

  handleAction(action: TableAction<T>): void {
    const item = this.$contextMenuItem();
    if (item) {
      this.closeContextMenu();

      if (action.danger) {
        this.$deleteConfirmationMessage.set(`Are you sure you want to delete this item?`);
        this.$showDeleteConfirmation.set(true);
        this.pendingDeleteAction = () => {
          action.handler(item);
          this.$contextMenuAction.emit({action, item});
        };
      } else {
        this.$contextMenuAction.emit({action, item});
        action.handler(item);
      }
    }
  }

  confirmDelete(): void {
    if (this.pendingDeleteAction) {
      this.pendingDeleteAction();
      this.pendingDeleteAction = null;
    }
    this.$showDeleteConfirmation.set(false);
  }

  cancelDelete(): void {
    this.pendingDeleteAction = null;
    this.$showDeleteConfirmation.set(false);
  }

  onReload(): void {
    this.$reload.emit();
  }

  getVisibleColumnCount(): number {
    return this.$visibleColumns().length;
  }

  getEmptyMessage(): string {
    return this.$searchQuery()
      ? `No results found for "${this.$searchQuery()}"`
      : this.$emptyMessage();
  }

  private getColumnValue(item: T, column: TableColumn<T>): any {
    if (column.value) return column.value(item);
    return item[column.key as keyof T];
  }
}
