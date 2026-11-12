import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { ApiRoutes, ServiceOrdersQuery } from '../../../core/api/api-routes';
import { Profile } from '../../../core/models/enums';
import {
  ServiceOrderDetailVm,
  ServiceOrderListItemVm,
} from '../models/service-orders.vm';

interface PlanOdometerDto {
  readonly odometerKm: number;
}

function odometerOf(plan: PlanOdometerDto): number | null {
  return plan.odometerKm ?? null;
}

function unknownOdometer(): Observable<number | null> {
  return of(null);
}

@Injectable({
  providedIn: 'root',
})
export class ServiceOrdersApi {
  private readonly apiClient: ApiClient = inject(ApiClient);

  getOrders(query?: ServiceOrdersQuery): Observable<readonly ServiceOrderListItemVm[]> {
    return this.apiClient.get<readonly ServiceOrderListItemVm[]>(
      ApiRoutes.serviceOrders.list(query)
    );
  }

  getOrderById(id: string, actor?: Profile): Observable<ServiceOrderDetailVm | null> {
    return this.apiClient.get<ServiceOrderDetailVm | null>(
      ApiRoutes.serviceOrders.byId(id, { actor })
    );
  }

  getVehicleOdometerKm(vehicleId: string): Observable<number | null> {
    return this.apiClient
      .get<PlanOdometerDto>(ApiRoutes.maintenance.planByVehicle(vehicleId))
      .pipe(map(odometerOf), catchError(unknownOdometer));
  }

  quote(
    id: string,
    body: { readonly amount: number; readonly quotedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.quote(id),
      body
    );
  }

  approve(id: string, body: { readonly approvedBy: string }): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.approve(id),
      body
    );
  }

  reject(id: string, body: { readonly rejectedBy: string }): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.reject(id),
      body
    );
  }

  start(id: string, body: { readonly startedBy: string }): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.start(id),
      body
    );
  }

  complete(
    id: string,
    body: { readonly completedBy: string; readonly servicedKm: number }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.complete(id),
      body
    );
  }
}
