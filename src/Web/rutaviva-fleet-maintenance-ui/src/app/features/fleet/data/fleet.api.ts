import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of, switchMap, throwError } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { ApiRoutes, VehiclesQuery } from '../../../core/api/api-routes';
import { ServiceOrderStatus, VehicleStatus } from '../../../core/models/enums';
import { VehicleDetailVm, VehicleSummaryVm } from '../models/fleet.vm';

export const VEHICLES_PAGE_SIZE = 200;
const HTTP_NOT_FOUND = 404;

export interface PagedResultDto<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

export interface VehicleSummaryDto {
  readonly id: string;
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly status: VehicleStatus;
  readonly customerId: string;
  readonly ownerName: string;
  readonly workshopId: string;
  readonly workshopName: string;
  readonly responsibleApproverId: string | null;
  readonly responsibleName: string | null;
}

export interface VehicleDetailDto {
  readonly id: string;
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly initialOdometerKm: number;
  readonly status: VehicleStatus;
  readonly owner: { readonly id: string; readonly name: string; readonly taxId: string };
  readonly workshop: { readonly id: string; readonly name: string };
  readonly responsible: {
    readonly id: string;
    readonly name: string;
    readonly email: string;
  } | null;
}

export interface VehicleServiceOrderSummaryDto {
  readonly id: string;
  readonly number: string;
  readonly vehicleId: string;
  readonly plate: string;
  readonly routineId: string;
  readonly routineName: string;
  readonly routineTypeCode: string;
  readonly status: ServiceOrderStatus;
  readonly statusBeforeHold: ServiceOrderStatus | null;
  readonly total: number | null;
  readonly appointmentAt: string | null;
}

export function mapVehicleSummaryDtoToVm(dto: VehicleSummaryDto): VehicleSummaryVm {
  return {
    vehicleId: dto.id,
    plate: dto.plate,
    vin: dto.vin ?? null,
    brand: dto.brand,
    model: dto.model,
    modelYear: dto.modelYear,
    status: dto.status,
    customerId: dto.customerId,
    customerName: dto.ownerName ?? '',
    workshopId: dto.workshopId,
    workshopName: dto.workshopName ?? '',
    responsibleApproverId: dto.responsibleApproverId ?? null,
    responsibleApproverName: dto.responsibleName ?? null,
  };
}

export function mapVehicleDetailDtoToVm(dto: VehicleDetailDto): VehicleDetailVm {
  return {
    vehicleId: dto.id,
    plate: dto.plate,
    vin: dto.vin ?? null,
    brand: dto.brand,
    model: dto.model,
    modelYear: dto.modelYear,
    customerId: dto.owner.id,
    customerName: dto.owner.name,
    workshopId: dto.workshop.id,
    workshopName: dto.workshop.name,
    responsibleApproverId: dto.responsible?.id ?? null,
    responsibleApproverName: dto.responsible?.name ?? null,
    status: dto.status,
    initialOdometerKm: dto.initialOdometerKm,
  };
}

function withPageSize(url: string): string {
  const separator: string = url.includes('?') ? '&' : '?';
  return `${url}${separator}pageSize=${VEHICLES_PAGE_SIZE}`;
}

function notFoundToNull(error: unknown): Observable<null> {
  if (error instanceof HttpErrorResponse && error.status === HTTP_NOT_FOUND) {
    return of(null);
  }
  return throwError(() => error);
}

@Injectable({
  providedIn: 'root',
})
export class FleetApi {
  private readonly apiClient: ApiClient = inject(ApiClient);

  getVehicles(query?: VehiclesQuery): Observable<readonly VehicleSummaryVm[]> {
    return this.apiClient
      .get<PagedResultDto<VehicleSummaryDto>>(withPageSize(ApiRoutes.fleet.vehicles(query)))
      .pipe(
        map((page: PagedResultDto<VehicleSummaryDto>) =>
          (page.items ?? []).map(mapVehicleSummaryDtoToVm)
        )
      );
  }

  getVehicleByPlate(plate: string): Observable<VehicleDetailVm | null> {
    return this.apiClient.get<VehicleDetailDto>(ApiRoutes.fleet.vehicleByPlate(plate)).pipe(
      map(mapVehicleDetailDtoToVm),
      catchError(notFoundToNull)
    );
  }

  updateVehicleStatus(
    vehicleId: string,
    plate: string,
    status: VehicleStatus
  ): Observable<VehicleDetailVm | null> {
    return this.apiClient
      .patch<{ readonly status: VehicleStatus }, void>(ApiRoutes.fleet.vehicleStatus(vehicleId), {
        status,
      })
      .pipe(switchMap(() => this.getVehicleByPlate(plate)));
  }
}
