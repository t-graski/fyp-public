import {Component, input} from '@angular/core';
import {CommonModule} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {ModuleDto} from '../../../api';

@Component({
  selector: 'app-module-info-tab',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './module-info-tab.component.html',
  styleUrl: './module-info-tab.component.scss'
})
export class ModuleInfoTabComponent {
  $module = input.required<ModuleDto>();

  private readonly dayLabels = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'] as const;

  formatDay(value: number | null | undefined): string {
    if (value == null) return '—';
    const idx = Number(value);
    return Number.isInteger(idx) && idx >= 0 && idx <= 6 ? this.dayLabels[idx] : String(value);
  }

  formatTime(value: string | null | undefined): string {
    if (!value) return '—';
    const [hourStr, minuteStr] = value.split(':');
    const hour = parseInt(hourStr, 10);
    const minute = minuteStr ?? '00';
    const period = hour >= 12 ? 'PM' : 'AM';
    const hour12 = hour % 12 === 0 ? 12 : hour % 12;
    return `${hour12}:${minute} ${period}`;
  }
}
