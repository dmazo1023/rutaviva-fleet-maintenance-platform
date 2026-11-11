import { Injectable, signal } from '@angular/core';

export const TOAST_DURATION_MS = 6000;

export type ToastTone = 'default' | 'warn' | 'danger';

export interface ToastAction {
  readonly label: string;
  readonly route?: string;
  readonly callback?: () => void;
}

export interface ToastItem {
  readonly id: string;
  readonly message: string;
  readonly detail: string | null;
  readonly icon: string;
  readonly tone: ToastTone;
  readonly isError: boolean;
  readonly persistent: boolean;
  readonly action: ToastAction | null;
}

export interface ShowToastOptions {
  readonly message: string;
  readonly detail?: string | null;
  readonly icon?: string;
  readonly tone?: ToastTone;
  readonly isError?: boolean;
  readonly persistent?: boolean;
  readonly action?: ToastAction | null;
}

@Injectable({
  providedIn: 'root',
})
export class ToastStore {
  private readonly itemsSignal = signal<readonly ToastItem[]>([]);
  private readonly timers = new Map<string, ReturnType<typeof setTimeout>>();
  private sequence = 0;

  readonly toasts = this.itemsSignal.asReadonly();

  show(options: ShowToastOptions): string {
    this.sequence += 1;
    const id: string = `toast-${this.sequence}`;
    const item: ToastItem = {
      id,
      message: options.message,
      detail: options.detail ?? null,
      icon: options.icon ?? (options.tone === 'danger' ? 'error' : 'check_circle'),
      tone: options.tone ?? 'default',
      isError: options.isError ?? options.tone === 'danger',
      persistent: options.persistent ?? false,
      action: options.action ?? null,
    };

    this.itemsSignal.update((current: readonly ToastItem[]) => [...current, item]);

    if (!item.persistent) {
      const timerId: ReturnType<typeof setTimeout> = setTimeout(() => {
        this.dismiss(id);
      }, TOAST_DURATION_MS);
      this.timers.set(id, timerId);
    }

    return id;
  }

  dismiss(id: string): void {
    const activeTimer: ReturnType<typeof setTimeout> | undefined = this.timers.get(id);
    if (activeTimer !== undefined) {
      clearTimeout(activeTimer);
      this.timers.delete(id);
    }
    this.itemsSignal.update((current: readonly ToastItem[]) =>
      current.filter((item: ToastItem) => item.id !== id)
    );
  }

  triggerAction(id: string): void {
    const target: ToastItem | undefined = this.itemsSignal().find(
      (item: ToastItem) => item.id === id
    );
    if (target?.action?.callback) {
      target.action.callback();
    }
    this.dismiss(id);
  }

  clearAll(): void {
    for (const timer of this.timers.values()) {
      clearTimeout(timer);
    }
    this.timers.clear();
    this.itemsSignal.set([]);
  }
}

