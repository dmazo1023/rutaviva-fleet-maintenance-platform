import { DueStatus, ServiceOrderStatus, TriggeredBy, VehicleStatus } from '../../../core/models/enums';

export interface VehicleNextMaintenanceVm {
  readonly routineName: string;
  readonly status: DueStatus;
  readonly consumedPercent: number;
  readonly remainingKind: 'km' | 'time';
  readonly remainingValue: number;
  readonly triggeredBy: TriggeredBy | null;
}

export interface VehicleOpenOrderSummaryVm {
  readonly orderId: string;
  readonly number: string;
  readonly status: ServiceOrderStatus;
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
  readonly worstConsumedPercent: number;
  readonly worstTriggeredBy: TriggeredBy | null;
  readonly odometerKm: number;
  readonly nextMaintenance: VehicleNextMaintenanceVm | null;
  readonly openOrder: VehicleOpenOrderSummaryVm | null;
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
  readonly odometerKm: number;
  readonly lastReadingText: string;
}

