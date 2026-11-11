import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { ApiRoutes, VehiclesQuery } from '../../../core/api/api-routes';
import { ServiceOrderStatus, VehicleStatus } from '../../../core/models/enums';
import { VehicleDetailVm } from '../models/fleet.vm';

export interface VehicleDto {
  readonly id: string;
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly customerId: string;
  readonly customerName: string;
  readonly workshopId: string;
  readonly workshopName: string;
  readonly responsibleApproverId: string | null;
  readonly responsibleApproverName: string | null;
  readonly status: VehicleStatus;
  readonly odometerKm: number;
  readonly lastReadingText?: string;
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

export function mapVehicleDtoToDetailVm(dto: VehicleDto): VehicleDetailVm {
  return {
    vehicleId: dto.id,
    plate: dto.plate,
    vin: dto.vin,
    brand: dto.brand,
    model: dto.model,
    modelYear: dto.modelYear,
    customerId: dto.customerId,
    customerName: dto.customerName,
    workshopId: dto.workshopId,
    workshopName: dto.workshopName,
    responsibleApproverId: dto.responsibleApproverId,
    responsibleApproverName: dto.responsibleApproverName,
    status: dto.status,
    odometerKm: dto.odometerKm,
    lastReadingText: dto.lastReadingText ?? 'hace 3 min',
  };
}

@Injectable({
  providedIn: 'root',
})
export class FleetApi {
  private readonly apiClient: ApiClient = inject(ApiClient);

  getVehicles(query?: VehiclesQuery): Observable<readonly VehicleDetailVm[]> {
    return this.apiClient
      .get<readonly VehicleDto[]>(ApiRoutes.fleet.vehicles(query))
      .pipe(map((dtos: readonly VehicleDto[]) => dtos.map(mapVehicleDtoToDetailVm)));
  }

  getVehicleByPlate(plate: string): Observable<VehicleDetailVm | null> {
    return this.apiClient
      .get<VehicleDto | null>(ApiRoutes.fleet.vehicleByPlate(plate))
      .pipe(map((dto: VehicleDto | null) => (dto ? mapVehicleDtoToDetailVm(dto) : null)));
  }

  updateVehicleStatus(vehicleId: string, status: VehicleStatus): Observable<VehicleDetailVm> {
    return this.apiClient
      .patch<{ readonly status: VehicleStatus }, VehicleDto>(
        ApiRoutes.fleet.vehicleStatus(vehicleId),
        { status }
      )
      .pipe(map(mapVehicleDtoToDetailVm));
  }

  getVehicleOrders(vehicleId: string): Observable<readonly VehicleServiceOrderSummaryDto[]> {
    return this.apiClient.get<readonly VehicleServiceOrderSummaryDto[]>(
      ApiRoutes.serviceOrders.list({ vehicleId })
    );
  }

  createManualServiceOrder(
    vehicleId: string,
    routineId: string
  ): Observable<{ readonly id: string; readonly number: string }> {
    return this.apiClient.post<
      { readonly vehicleId: string; readonly routineId: string },
      { readonly id: string; readonly number: string }
    >(ApiRoutes.serviceOrders.create(), { vehicleId, routineId });
  }
}

