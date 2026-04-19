import {Component, model, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {MatIconModule} from '@angular/material/icon';

export interface DateRange {
  from: string;
  to: string;
}

@Component({
  selector: 'app-date-range-picker',
  imports: [
    FormsModule,
    MatIconModule
  ],
  templateUrl: './date-range-picker.html',
  styleUrl: './date-range-picker.scss'
})
export class DateRangePicker {
  $dateRange = model.required<DateRange>();

  /**
   * Quick range state:
   *  - null: manual/custom
   *  - 30/90/...: last N days
   *  - -1: "This week" preset (only when the selected range matches the current week)
   *  - -2: Week navigation (previous/next week from the currently selected week)
   */
  readonly $activeQuickRangeDays = signal<number | null>(null);

  onFromChange(value: string) {
    this.$dateRange.update(range => ({...range, from: value}));
    this.syncPresetStateWithRange();
  }

  onToChange(value: string) {
    this.$dateRange.update(range => ({...range, to: value}));
    this.syncPresetStateWithRange();
  }

  setThisWeek() {
    const {from, to} = this.getWeekRangeForDate(new Date());
    this.$activeQuickRangeDays.set(-1);
    this.$dateRange.set({from, to});
  }

  previousWeek() {
    this.$activeQuickRangeDays.set(-2);
    const anchor = this.getSelectedWeekAnchorDate();
    anchor.setDate(anchor.getDate() - 7);
    const {from, to} = this.getWeekRangeForDate(anchor);
    this.$dateRange.set({from, to});
  }

  nextWeek() {
    this.$activeQuickRangeDays.set(-2);
    const anchor = this.getSelectedWeekAnchorDate();
    anchor.setDate(anchor.getDate() + 7);
    const {from, to} = this.getWeekRangeForDate(anchor);
    this.$dateRange.set({from, to});
  }

  setQuickRange(days: number) {
    if (days === 7) {
      this.setThisWeek();
      return;
    }

    this.$activeQuickRangeDays.set(days);

    const to = new Date();
    const from = new Date();
    from.setDate(to.getDate() - days);

    this.$dateRange.set({
      from: this.formatDate(from),
      to: this.formatDate(to)
    });
  }

  isQuickRangeActive(days: number): boolean {
    if (days === 7) {
      return this.$activeQuickRangeDays() === -1;
    }

    return this.$activeQuickRangeDays() === days;
  }

  isWeekMode(): boolean {
    return this.$activeQuickRangeDays() === -1 || this.$activeQuickRangeDays() === -2;
  }

  private getSelectedWeekAnchorDate(): Date {
    const fromIso = this.$dateRange().from;
    const parsed = this.parseIsoDate(fromIso);
    return parsed ?? new Date();
  }

  private getWeekRangeForDate(date: Date): { from: string; to: string } {
    const base = new Date(date.getFullYear(), date.getMonth(), date.getDate());

    const day = base.getDay();
    const diffToMonday = (day + 6) % 7;

    const weekStart = new Date(base);
    weekStart.setDate(base.getDate() - diffToMonday);

    const weekEnd = new Date(weekStart);
    weekEnd.setDate(weekStart.getDate() + 6);

    return {
      from: this.formatDate(weekStart),
      to: this.formatDate(weekEnd)
    };
  }

  private parseIsoDate(value?: string | null): Date | null {
    if (!value) return null;
    // Expecting YYYY-MM-DD
    const match = /^\d{4}-\d{2}-\d{2}$/.test(value);
    if (!match) return null;

    const [y, m, d] = value.split('-').map(Number);
    if (!y || !m || !d) return null;

    return new Date(y, m - 1, d);
  }

  private formatDate(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  private syncPresetStateWithRange(): void {
    const active = this.$activeQuickRangeDays();
    if (active != null && active > 0) return;

    const currentWeek = this.getWeekRangeForDate(new Date());
    const range = this.$dateRange();

    if (range.from === currentWeek.from && range.to === currentWeek.to) {
      this.$activeQuickRangeDays.set(-1);
      return;
    }

    this.$activeQuickRangeDays.set(null);
  }
}
