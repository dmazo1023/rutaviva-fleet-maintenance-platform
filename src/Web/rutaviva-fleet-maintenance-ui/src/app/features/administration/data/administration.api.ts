import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { ApiRoutes } from '../../../core/api/api-routes';
import { RoutineTypeStatus } from '../../../core/models/enums';
import { ApprovalPolicyVm, RoutineTypeVm } from '../models/administration.vm';

interface RoutineTypeDto {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly defaultIntervalKm: number | null;
  readonly defaultIntervalMonths: number | null;
  readonly dueSoonPercent: number;
  readonly status: RoutineTypeStatus;
}

export interface UpdateRoutineTypeRequest {
  readonly intervalKm: number | null;
  readonly intervalMonths: number | null;
  readonly dueSoonThresholdPercent: number;
  readonly updatedBy: string;
}

function toRoutineTypeVm(dto: RoutineTypeDto): RoutineTypeVm {
  return {
    id: dto.id,
    code: dto.code,
    name: dto.name,
    defaultIntervalKm: dto.defaultIntervalKm,
    defaultIntervalMonths: dto.defaultIntervalMonths,
    dueSoonThresholdPercent: dto.dueSoonPercent,
    status: dto.status,
  };
}

@Injectable({
  providedIn: 'root',
})
export class AdministrationApi {
  private readonly apiClient: ApiClient = inject(ApiClient);

  getRoutineTypes(): Observable<readonly RoutineTypeVm[]> {
    return this.apiClient
      .get<readonly RoutineTypeDto[]>(ApiRoutes.maintenance.routineTypes())
      .pipe(map((dtos: readonly RoutineTypeDto[]) => dtos.map(toRoutineTypeVm)));
  }

  updateRoutineType(id: string, body: UpdateRoutineTypeRequest): Observable<void> {
    return this.apiClient.put<UpdateRoutineTypeRequest, void>(
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
