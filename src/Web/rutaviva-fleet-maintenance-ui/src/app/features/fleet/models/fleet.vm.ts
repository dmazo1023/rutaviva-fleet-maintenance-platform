import { DueStatus, VehicleStatus } from '../../../core/models/enums';

export interface VehicleSummaryVm {
  readonly vehicleId: string;
  readonly plate: string;
  readonly vin: string | null;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly status: VehicleStatus;
  readonly customerId: string;
  readonly customerName: string;
  readonly workshopId: string;
  readonly workshopName: string;
  readonly responsibleApproverId: string | null;
  readonly responsibleApproverName: string | null;
}

export interface VehicleMaintenanceSummaryVm {
  readonly worstDueStatus: DueStatus | null;
  readonly maxConsumedPercent: number;
  readonly upToDateCount: number;
  readonly dueSoonCount: number;
  readonly overdueCount: number;
}

export interface VehicleListItemVm {
  readonly vehicleId: string;
  readonly plate: string;
  readonly brand: string;
  readonly model: string;
  readonly modelYear: number;
  readonly customerId: string;
  readonly customerName: string;
  readonly status: VehicleStatus;
  readonly worstDueStatus: DueStatus | null;
  readonly maxConsumedPercent: number;
  readonly odometerKm: number | null;
  readonly maintenance: VehicleMaintenanceSummaryVm | null;
}

export interface VehicleDetailVm {
  readonly vehicleId: string;
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
  readonly initialOdometerKm: number;
}
