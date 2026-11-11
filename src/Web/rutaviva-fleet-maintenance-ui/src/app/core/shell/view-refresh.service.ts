import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ViewRefreshService {
  private readonly refreshSubject = new Subject<void>();
  private activeVehicleId: string | null = null;

  readonly refreshRequested$: Observable<void> = this.refreshSubject.asObservable();
  readonly refresh$: Observable<void> = this.refreshRequested$;

  setActiveVehicleId(vehicleId: string | null): void {
    this.activeVehicleId = vehicleId;
  }

  getActiveVehicleId(): string | null {
    return this.activeVehicleId;
  }

  requestRefresh(): void {
    this.refreshSubject.next();
  }
}
