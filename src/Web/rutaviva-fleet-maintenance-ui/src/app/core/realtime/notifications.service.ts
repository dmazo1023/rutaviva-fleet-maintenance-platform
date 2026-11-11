import { inject, Injectable, InjectionToken, signal, Signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Profile } from '../models/enums';
import { HubMethods } from './hub-methods';
import {
  AlertChangedEvent,
  ApprovalRequestedEvent,
  OdometerReadingRecordedEvent,
  RoutineChangedEvent,
  ServiceOrderChangedEvent,
  VehicleChangedEvent,
} from './realtime-events';

export type RealtimeConnectionState = 'connected' | 'reconnecting' | 'disconnected';

export interface HubConnectionLike {
  start(): Promise<void>;
  stop(): Promise<void>;
  invoke(methodName: string, ...args: unknown[]): Promise<unknown>;
  on(methodName: string, newMethod: (...args: unknown[]) => void): void;
  onreconnecting(callback: (error?: Error) => void): void;
  onreconnected(callback: (connectionId?: string) => void): void;
  onclose(callback: (error?: Error) => void): void;
}

export const HUB_CONNECTION_FACTORY = new InjectionToken<() => HubConnectionLike | null>(
  'HUB_CONNECTION_FACTORY',
  {
    providedIn: 'root',
    factory: (): (() => HubConnectionLike | null) => {
      return (): HubConnectionLike | null => {
        if (environment.useMockBackend) {
          return null;
        }
        const url: string = `${environment.apiBaseUrl}${HubMethods.endpoint}`;
        const connection: HubConnection = new HubConnectionBuilder()
          .withUrl(url)
          .withAutomaticReconnect()
          .build();
        return connection;
      };
    },
  }
);

@Injectable({
  providedIn: 'root',
})
export class NotificationsService {
  private readonly connectionFactory: () => HubConnectionLike | null =
    inject(HUB_CONNECTION_FACTORY);
  private hubConnection: HubConnectionLike | null = null;
  private joinedCustomerId: string | null = null;
  private joinedApproverId: string | null = null;
  private started = false;

  private readonly connectionStateSignal = signal<RealtimeConnectionState>(
    environment.useMockBackend ? 'connected' : 'disconnected'
  );

  private readonly reconnectedSubject = new Subject<void>();
  private readonly routineChangedSubject = new Subject<RoutineChangedEvent>();
  private readonly odometerReadingRecordedSubject = new Subject<OdometerReadingRecordedEvent>();
  private readonly vehicleChangedSubject = new Subject<VehicleChangedEvent>();
  private readonly alertChangedSubject = new Subject<AlertChangedEvent>();
  private readonly serviceOrderChangedSubject = new Subject<ServiceOrderChangedEvent>();
  private readonly approvalRequestedSubject = new Subject<ApprovalRequestedEvent>();

  readonly connectionState: Signal<RealtimeConnectionState> =
    this.connectionStateSignal.asReadonly();

  readonly reconnected$: Observable<void> = this.reconnectedSubject.asObservable();
  readonly routineChanged$: Observable<RoutineChangedEvent> =
    this.routineChangedSubject.asObservable();
  readonly odometerReadingRecorded$: Observable<OdometerReadingRecordedEvent> =
    this.odometerReadingRecordedSubject.asObservable();
  readonly vehicleChanged$: Observable<VehicleChangedEvent> =
    this.vehicleChangedSubject.asObservable();
  readonly alertChanged$: Observable<AlertChangedEvent> = this.alertChangedSubject.asObservable();
  readonly serviceOrderChanged$: Observable<ServiceOrderChangedEvent> =
    this.serviceOrderChangedSubject.asObservable();
  readonly approvalRequested$: Observable<ApprovalRequestedEvent> =
    this.approvalRequestedSubject.asObservable();

  async start(): Promise<void> {
    if (this.started) {
      return;
    }
    this.started = true;
    this.hubConnection = this.connectionFactory();

    if (!this.hubConnection) {
      this.connectionStateSignal.set('connected');
      return;
    }

    this.registerHandlers(this.hubConnection);

    try {
      await this.hubConnection.start();
      this.connectionStateSignal.set('connected');
    } catch {
      this.connectionStateSignal.set('disconnected');
    }
  }

  async syncSession(
    customerId: string,
    profile: Profile,
    actorId: string | null
  ): Promise<void> {
    if (!this.started) {
      await this.start();
    }

    if (!this.hubConnection || this.connectionStateSignal() !== 'connected') {
      this.joinedCustomerId = customerId || null;
      this.joinedApproverId = profile === 'Approver' ? actorId : null;
      return;
    }

    const nextCustomerId: string | null = customerId || null;
    if (this.joinedCustomerId && this.joinedCustomerId !== nextCustomerId) {
      await this.safeInvoke(HubMethods.client.leaveCustomer, this.joinedCustomerId);
    }
    if (nextCustomerId && this.joinedCustomerId !== nextCustomerId) {
      await this.safeInvoke(HubMethods.client.joinCustomer, nextCustomerId);
    }
    this.joinedCustomerId = nextCustomerId;

    const nextApproverId: string | null = profile === 'Approver' ? actorId : null;
    if (this.joinedApproverId && this.joinedApproverId !== nextApproverId) {
      await this.safeInvoke(HubMethods.client.leaveApprover, this.joinedApproverId);
    }
    if (nextApproverId && this.joinedApproverId !== nextApproverId) {
      await this.safeInvoke(HubMethods.client.joinApprover, nextApproverId);
    }
    this.joinedApproverId = nextApproverId;
  }

  setConnectionStateForTest(state: RealtimeConnectionState): void {
    this.connectionStateSignal.set(state);
  }

  emitRoutineChanged(event: RoutineChangedEvent): void {
    this.routineChangedSubject.next(event);
  }

  emitOdometerReadingRecorded(event: OdometerReadingRecordedEvent): void {
    this.odometerReadingRecordedSubject.next(event);
  }

  emitVehicleChanged(event: VehicleChangedEvent): void {
    this.vehicleChangedSubject.next(event);
  }

  emitAlertChanged(event: AlertChangedEvent): void {
    this.alertChangedSubject.next(event);
  }

  emitServiceOrderChanged(event: ServiceOrderChangedEvent): void {
    this.serviceOrderChangedSubject.next(event);
  }

  emitApprovalRequested(event: ApprovalRequestedEvent): void {
    this.approvalRequestedSubject.next(event);
  }

  private registerHandlers(connection: HubConnectionLike): void {
    connection.onreconnecting(() => {
      this.connectionStateSignal.set('reconnecting');
    });

    connection.onreconnected(() => {
      this.connectionStateSignal.set('connected');
      if (this.joinedCustomerId) {
        void this.safeInvoke(HubMethods.client.joinCustomer, this.joinedCustomerId);
      }
      if (this.joinedApproverId) {
        void this.safeInvoke(HubMethods.client.joinApprover, this.joinedApproverId);
      }
      this.reconnectedSubject.next();
    });

    connection.onclose(() => {
      this.connectionStateSignal.set('disconnected');
    });

    connection.on(HubMethods.server.routineChanged, (payload: unknown) => {
      this.routineChangedSubject.next(payload as RoutineChangedEvent);
    });

    connection.on(HubMethods.server.odometerReadingRecorded, (payload: unknown) => {
      this.odometerReadingRecordedSubject.next(payload as OdometerReadingRecordedEvent);
    });

    connection.on(HubMethods.server.vehicleChanged, (payload: unknown) => {
      this.vehicleChangedSubject.next(payload as VehicleChangedEvent);
    });

    connection.on(HubMethods.server.alertChanged, (payload: unknown) => {
      this.alertChangedSubject.next(payload as AlertChangedEvent);
    });

    connection.on(HubMethods.server.serviceOrderChanged, (payload: unknown) => {
      this.serviceOrderChangedSubject.next(payload as ServiceOrderChangedEvent);
    });

    connection.on(HubMethods.server.approvalRequested, (payload: unknown) => {
      this.approvalRequestedSubject.next(payload as ApprovalRequestedEvent);
    });
  }

  private async safeInvoke(methodName: string, arg: string): Promise<void> {
    if (!this.hubConnection) {
      return;
    }
    try {
      await this.hubConnection.invoke(methodName, arg);
    } catch {
      return;
    }
  }
}

