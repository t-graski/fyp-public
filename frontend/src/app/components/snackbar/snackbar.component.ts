import { Component, inject } from '@angular/core';
import { SnackbarService } from '../../services/snackbar.service';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-snackbar',
  imports: [CommonModule, MatIconModule],
  templateUrl: './snackbar.component.html',
  styleUrl: './snackbar.component.scss'
})
export class SnackbarComponent {
  private readonly snackbarService = inject(SnackbarService);
  messages$ = this.snackbarService.messages$;

  getSnackbarClass(statusCode: number): string {
    if (statusCode >= 200 && statusCode < 300) return 'snackbar--success';
    if (statusCode >= 400 && statusCode < 500) return 'snackbar--warning';
    if (statusCode >= 500) return 'snackbar--error';
    return 'snackbar--info';
  }

  getIcon(statusCode: number): string {
    if (statusCode >= 200 && statusCode < 300) return 'check_circle';
    if (statusCode >= 400 && statusCode < 500) return 'warning';
    if (statusCode >= 500) return 'error';
    return 'info';
  }

  close(id: number): void {
    this.snackbarService.remove(id);
  }
}

