import {Component, computed, EventEmitter, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {BulkResultItem} from './bulk-result-dialog.models';

@Component({
  selector: 'app-bulk-result-dialog',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './bulk-result-dialog.component.html',
  styleUrl: './bulk-result-dialog.component.scss'
})
export class BulkResultDialogComponent {
  items = signal<BulkResultItem[]>([]);

  onClose = new EventEmitter<void>();

  $succeeded = computed(() => this.items().filter(i => i.success).length);
  $failed = computed(() => this.items().filter(i => !i.success).length);
  $filter = signal<'all' | 'success' | 'failed'>('all');

  $filtered = computed(() => {
    const f = this.$filter();
    const items = this.items();
    if (f === 'success') return items.filter(i => i.success);
    if (f === 'failed') return items.filter(i => !i.success);
    return items;
  });

  close(): void {
    this.onClose.emit();
  }
}
