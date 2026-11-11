import { TestBed } from '@angular/core/testing';
import { HubMethods } from './hub-methods';
import {
  HUB_CONNECTION_FACTORY,
  HubConnectionLike,
  NotificationsService,
} from './notifications.service';

class FakeHubConnection implements HubConnectionLike {
  readonly invoked: { readonly method: string; readonly args: readonly unknown[] }[] = [];
  private reconnectingHandler: ((error?: Error) => void) | null = null;
  private reconnectedHandler: ((connectionId?: string) => void) | null = null;
  private closeHandler: ((error?: Error) => void) | null = null;
  private readonly handlers = new Map<string, (...args: unknown[]) => void>();

  async start(): Promise<void> {
    return Promise.resolve();
  }

  async stop(): Promise<void> {
    return Promise.resolve();
  }

  async invoke(methodName: string, ...args: unknown[]): Promise<unknown> {
    this.invoked.push({ method: methodName, args });
    return Promise.resolve();
  }

  on(methodName: string, newMethod: (...args: unknown[]) => void): void {
    this.handlers.set(methodName, newMethod);
  }

  onreconnecting(callback: (error?: Error) => void): void {
    this.reconnectingHandler = callback;
  }

  onreconnected(callback: (connectionId?: string) => void): void {
    this.reconnectedHandler = callback;
  }

  onclose(callback: (error?: Error) => void): void {
    this.closeHandler = callback;
  }

  triggerReconnecting(): void {
    this.reconnectingHandler?.();
  }

  triggerReconnected(): void {
    this.reconnectedHandler?.('conn-1');
  }

  triggerClose(): void {
    this.closeHandler?.();
  }
}

describe('NotificationsService', () => {
  let fakeHub: FakeHubConnection;
  let service: NotificationsService;

  beforeEach(() => {
    fakeHub = new FakeHubConnection();
    TestBed.configureTestingModule({
      providers: [
        {
          provide: HUB_CONNECTION_FACTORY,
          useValue: (): HubConnectionLike => fakeHub,
        },
      ],
    });
    service = TestBed.inject(NotificationsService);
  });

  it('should invoke LeaveCustomer, JoinCustomer, and JoinApprover when session changes', async () => {
    await service.syncSession('900000001', 'Operator', null);
    expect(fakeHub.invoked).toEqual([
      { method: HubMethods.client.joinCustomer, args: ['900000001'] },
    ]);

    fakeHub.invoked.length = 0;
    await service.syncSession('800200300', 'Approver', 'approver-camila');
    expect(fakeHub.invoked).toEqual([
      { method: HubMethods.client.leaveCustomer, args: ['900000001'] },
      { method: HubMethods.client.joinCustomer, args: ['800200300'] },
      { method: HubMethods.client.joinApprover, args: ['approver-camila'] },
    ]);
  });

  it('should update connectionState and emit reconnected$ on reconnection', async () => {
    let reconnectedCount = 0;
    service.reconnected$.subscribe(() => {
      reconnectedCount += 1;
    });

    await service.syncSession('900000001', 'Approver', 'approver-laura');
    expect(service.connectionState()).toBe('connected');

    fakeHub.triggerReconnecting();
    expect(service.connectionState()).toBe('reconnecting');

    fakeHub.invoked.length = 0;
    fakeHub.triggerReconnected();
    expect(service.connectionState()).toBe('connected');
    expect(reconnectedCount).toBe(1);
    expect(fakeHub.invoked).toEqual([
      { method: HubMethods.client.joinCustomer, args: ['900000001'] },
      { method: HubMethods.client.joinApprover, args: ['approver-laura'] },
    ]);
  });
});

