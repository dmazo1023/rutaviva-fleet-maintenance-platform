import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { ApiRoutes } from '../../../core/api/api-routes';
import { ApprovalPolicyVm, RoutineTypeVm } from '../models/administration.vm';

export interface UpdateRoutineTypeRequest {
  readonly defaultIntervalKm: number | null;
  readonly defaultIntervalMonths: number | null;
  readonly dueSoonThresholdPercent: number;
}

@Injectable({
  providedIn: 'root',
})
export class AdministrationApi {
  private readonly apiClient: ApiClient = inject(ApiClient);

  getRoutineTypes(): Observable<readonly RoutineTypeVm[]> {
    return this.apiClient.get<readonly RoutineTypeVm[]>(ApiRoutes.maintenance.routineTypes());
  }

  updateRoutineType(id: string, body: UpdateRoutineTypeRequest): Observable<RoutineTypeVm> {
    return this.apiClient.put<UpdateRoutineTypeRequest, RoutineTypeVm>(
      ApiRoutes.maintenance.routineTypeById(id),
      body
    );
  }

  getApprovalPolicy(): Observable<ApprovalPolicyVm> {
    return this.apiClient.get<ApprovalPolicyVm>(ApiRoutes.serviceOrders.approvalPolicies());
  }

  updateApprovalPolicy(
    autoApprovalMaxAmount: number,
    createdBy: string
  ): Observable<ApprovalPolicyVm> {
    return this.apiClient.post<
      { readonly autoApprovalMaxAmount: number; readonly createdBy: string },
      ApprovalPolicyVm
    >(ApiRoutes.serviceOrders.approvalPolicies(), {
      autoApprovalMaxAmount,
      createdBy,
    });
  }
}

