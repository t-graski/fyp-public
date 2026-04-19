import {Injectable} from '@angular/core';
import {BehaviorSubject} from 'rxjs';
import {ObjectApiResponse} from '../api';

export interface SnackbarMessage {
  message: string;
  statusCode: number;
  errorCode?: string;
  id: number;
  removing?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class SnackbarService {
  private messagesSubject = new BehaviorSubject<SnackbarMessage[]>([]);
  public messages$ = this.messagesSubject.asObservable();
  private messageId = 0;
  private readonly MAX_MESSAGES = 5;

  show(message: string, statusCode: number = 200, errorCode?: string): void {
    const id = this.messageId++;
    const snackbar: SnackbarMessage = {message, statusCode, errorCode, id, removing: false};

    let current = this.messagesSubject.value;

    if (current.length >= this.MAX_MESSAGES) {
      const oldestId = current[0].id;
      this.remove(oldestId);
    }

    current = this.messagesSubject.value;
    this.messagesSubject.next([...current, snackbar]);

    setTimeout(() => {
      this.remove(id);
    }, 5000);
  }

  showFromApiResponse(response: ObjectApiResponse): void {
    const statusCode = response?.statusCode ?? 200;
    const message = response?.error?.message ?? "Operation completed.";
    const errorCode = response?.error?.code || (statusCode >= 400 ? `ERROR_${statusCode}` : undefined);
    this.show(message, statusCode, errorCode);
  }

  remove(id: number): void {
    const current = this.messagesSubject.value;
    const updated = current.map(m => m.id === id ? {...m, removing: true} : m);
    this.messagesSubject.next(updated);

    setTimeout(() => {
      const filtered = this.messagesSubject.value.filter(m => m.id !== id);
      this.messagesSubject.next(filtered);
    }, 300);
  }
}
