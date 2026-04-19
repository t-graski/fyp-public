import {Component, Input} from '@angular/core';
import {NgStyle} from '@angular/common';

@Component({
  selector: 'app-progress-bar',
  imports: [
    NgStyle
  ],
  templateUrl: './progress-bar.html',
  styleUrl: './progress-bar.scss',
})
export class ProgressBar {
  @Input() value: number = 0;
  @Input() color: string = "#ffffff";
  @Input() showValue = false;

  clamp(v: number): number {
    return Math.max(0, Math.min(100, v));
  }
}
