import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { ApiRoutes, ServiceOrdersQuery } from '../../../core/api/api-routes';
import { Profile } from '../../../core/models/enums';
import {
  ServiceOrderDetailVm,
  ServiceOrderListItemVm,
} from '../models/service-orders.vm';

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
    const base: string = ApiRoutes.serviceOrders.byId(id);
    const url: string = actor ? `${base}?actor=${encodeURIComponent(actor)}` : base;
    return this.apiClient.get<ServiceOrderDetailVm | null>(url);
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

  approve(
    id: string,
    body: { readonly approvedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.approve(id),
      body
    );
  }

  reject(
    id: string,
    body: { readonly rejectedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.reject(id),
      body
    );
  }

  cancel(
    id: string,
    body: { readonly cancelledBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.cancel(id),
      body
    );
  }

  scheduleAppointment(
    id: string,
    body: { readonly appointmentAt: string; readonly scheduledBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.put<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.appointment(id),
      body
    );
  }

  markOnSite(
    id: string,
    body: { readonly markedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.onSite(id),
      body
    );
  }

  start(
    id: string,
    body: { readonly startedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.start(id),
      body
    );
  }

  complete(
    id: string,
    body: { readonly completedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.complete(id),
      body
    );
  }

  addAdditionalService(
    id: string,
    body: { readonly description: string; readonly amount: number; readonly addedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.addAdditionalService(id),
      body
    );
  }

  requestAdditionalService(
    id: string,
    body: { readonly description: string; readonly requestedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.requestAdditionalService(id),
      body
    );
  }

  priceAdditionalService(
    id: string,
    lineId: string,
    body: { readonly amount: number; readonly pricedBy: string }
  ): Observable<ServiceOrderDetailVm> {
    return this.apiClient.post<typeof body, ServiceOrderDetailVm>(
      ApiRoutes.serviceOrders.priceAdditionalService(id, lineId),
      body
    );
  }
}
