import {
  Component,
  ElementRef,
  inject,
  input,
  OnInit,
  output,
  signal,
  ViewChild,
  AfterViewChecked
} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {MatIconModule} from '@angular/material/icon';
import {ModuleChatService} from '../../../api';
import {ChatMessageDto} from '../../../api';
import {ThreadSummaryDto} from '../../../api';

@Component({
  selector: 'app-module-ai-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule],
  templateUrl: './module-ai-chat.component.html',
  styleUrl: './module-ai-chat.component.scss'
})
export class ModuleAiChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('inputField') inputField!: ElementRef<HTMLTextAreaElement>;

  $moduleId = input.required<string>();
  $isOpen = input.required<boolean>();
  isOpenChange = output<boolean>();
  highlightElement = output<string>();

  private readonly chatService = inject(ModuleChatService);

  $isLoading = signal(false);
  $isSending = signal(false);
  $threads = signal<ThreadSummaryDto[]>([]);
  $activeThreadId = signal<string | null>(null);
  $messages = signal<ChatMessageDto[]>([]);
  $messageInput = signal('');
  $showThreadList = signal(false);
  // key'd by assistantMessageId -> highlightElementId from SendMessageResponse
  $highlightMap = signal<Record<string, string>>({});

  private shouldScrollToBottom = false;
  private threadsLoaded = false;

  ngOnInit(): void {
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  open(): void {
    this.isOpenChange.emit(true);
    if (!this.threadsLoaded) {
      this.loadThreads();
    }
  }

  close(): void {
    this.isOpenChange.emit(false);
    this.$showThreadList.set(false);
  }

  loadThreads(): void {
    this.threadsLoaded = true;
    this.$isLoading.set(true);
    this.chatService.apiModulesModuleIdChatThreadsGet(this.$moduleId()).subscribe({
      next: (res) => {
        const threads = res.data ?? [];
        this.$threads.set(threads);
        this.$isLoading.set(false);
        if (threads.length > 0) {
          this.selectThread(threads[0].id!);
        }
      },
      error: () => {
        this.$isLoading.set(false);
      }
    });
  }

  selectThread(threadId: string): void {
    this.$activeThreadId.set(threadId);
    this.$showThreadList.set(false);
    this.loadMessages(threadId);
  }

  loadMessages(threadId: string): void {
    this.$isLoading.set(true);
    this.chatService.apiModulesModuleIdChatThreadsThreadIdMessagesGet(
      this.$moduleId(), threadId
    ).subscribe({
      next: (res) => {
        this.$messages.set(res.data ?? []);
        this.$isLoading.set(false);
        this.shouldScrollToBottom = true;
      },
      error: () => {
        this.$isLoading.set(false);
      }
    });
  }

  createNewThread(): void {
    this.$isLoading.set(true);
    this.chatService.apiModulesModuleIdChatThreadsPost(this.$moduleId()).subscribe({
      next: (res) => {
        const threadId = res.data?.threadId;
        if (threadId) {
          this.$threads.update(threads => [
            {id: threadId, title: 'New chat', updatedAtUtc: new Date().toISOString()},
            ...threads
          ]);
          this.$activeThreadId.set(threadId);
          this.$messages.set([]);
          this.$showThreadList.set(false);
        }
        this.$isLoading.set(false);
      },
      error: () => {
        this.$isLoading.set(false);
      }
    });
  }

  sendMessage(): void {
    const message = this.$messageInput().trim();
    if (!message || this.$isSending()) return;

    if (!this.$activeThreadId()) {
      this.chatService.apiModulesModuleIdChatThreadsPost(this.$moduleId()).subscribe({
        next: (res) => {
          const threadId = res.data?.threadId;
          if (threadId) {
            this.$threads.update(threads => [
              {id: threadId, title: 'New chat', updatedAtUtc: new Date().toISOString()},
              ...threads
            ]);
            this.$activeThreadId.set(threadId);
            this.doSendMessage(threadId, message);
          }
        }
      });
      return;
    }

    this.doSendMessage(this.$activeThreadId()!, message);
  }

  private doSendMessage(threadId: string, message: string): void {
    this.$messageInput.set('');
    this.$isSending.set(true);

    const userMsg: ChatMessageDto = {
      id: 'temp-' + Date.now(),
      role: 'user',
      content: message,
      createdAtUtc: new Date().toISOString()
    };
    this.$messages.update(msgs => [...msgs, userMsg]);
    this.shouldScrollToBottom = true;

    this.chatService.apiModulesModuleIdChatThreadsThreadIdMessagesPost(
      this.$moduleId(), threadId, {message}
    ).subscribe({
      next: (res) => {
        const data = res.data;
        if (data) {
          const assistantMsg: ChatMessageDto = {
            id: data.assistantMessageId,
            role: 'assistant',
            content: data.answer,
            sources: data.sources,
            createdAtUtc: new Date().toISOString()
          };
          this.$messages.update(msgs => [...msgs, assistantMsg]);
          if (data.assistantMessageId && data.highlightElementId) {
            this.$highlightMap.update(m => ({
              ...m,
              [data.assistantMessageId!]: data.highlightElementId!
            }));
          }
          this.$threads.update(threads =>
            threads.map(t => t.id === threadId
              ? {...t, updatedAtUtc: new Date().toISOString()}
              : t)
          );
        }
        this.$isSending.set(false);
        this.shouldScrollToBottom = true;
      },
      error: (err) => {
        const code = err.error.error.code;
        console.log(err);
        const messageText = code === 'MODULE_NOT_INDEXED'
          ? 'This module has not been indexed yet. Please message a tutor or staff member so they can index it first.'
          : 'Sorry — I could not send that message right now. Please try again.';

        this.$messages.update(msgs => [...msgs, {
          id: 'system-' + Date.now(),
          role: 'assistant',
          content: messageText,
          createdAtUtc: new Date().toISOString()
        }]);
        this.$isSending.set(false);
        this.shouldScrollToBottom = true;
      }
    });
  }

  onInputKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  onInputChange(event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.$messageInput.set(value);
    this.autoResizeTextarea(event.target as HTMLTextAreaElement);
  }

  private autoResizeTextarea(el: HTMLTextAreaElement): void {
    el.style.height = 'auto';
    el.style.height = Math.min(Math.max(el.scrollHeight, 44), 140) + 'px';
  }

  toggleThreadList(): void {
    this.$showThreadList.update(v => !v);
  }

  emitHighlight(elementId: string): void {
    this.highlightElement.emit(elementId);
  }

  private scrollToBottom(): void {
    if (this.messagesContainer) {
      this.messagesContainer.nativeElement.scrollTop =
        this.messagesContainer.nativeElement.scrollHeight;
    }
  }
}
