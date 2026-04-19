import {Injectable, signal} from '@angular/core';
import {ContextMenuAction} from '../components/context-menu/context-menu.component';

export interface ContextMenuState {
  isOpen: boolean;
  position: { x: number; y: number };
  actions: ContextMenuAction[];
}

@Injectable({
  providedIn: 'root'
})
export class ContextMenuService {
  private readonly state = signal<ContextMenuState>({
    isOpen: false,
    position: {x: 0, y: 0},
    actions: []
  });

  readonly $state = this.state.asReadonly();

  open(event: MouseEvent, actions: ContextMenuAction[]): void {
    event.preventDefault();
    event.stopPropagation();

    this.state.set({
      isOpen: true,
      position: {x: event.clientX, y: event.clientY},
      actions
    });
  }

  close(): void {
    this.state.update(state => ({...state, isOpen: false}));
  }
}
