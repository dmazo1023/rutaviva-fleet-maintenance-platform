import {
  AlertDto,
  CustomerDetailDto,
  CustomerSummaryDto,
  RoutineTypeDto,
  VehicleDetailDto,
  VehicleSummaryDto,
  WorkshopDetailDto,
  WorkshopSummaryDto,
} from './mock-backend.dtos';
import {
  MockApproverRecord,
  MockCustomerRecord,
  MockRoutineTypeRecord,
  MockVehicleRecord,
  MockWorkshopRecord,
} from './mock-backend.seed';
import { AlertChangedEvent } from '../realtime/realtime-events';

export interface VehicleRelations {
  readonly customer: MockCustomerRecord;
  readonly workshop: MockWorkshopRecord;
  readonly responsible: MockApproverRecord | null;
}

export function toCustomerSummary(customer: MockCustomerRecord): CustomerSummaryDto {
  return {
    id: customer.id,
    name: customer.name,
    taxId: customer.taxId,
    email: customer.email,
    status: customer.status,
  };
}

export function toCustomerDetail(customer: MockCustomerRecord): CustomerDetailDto {
  return {
    ...toCustomerSummary(customer),
    phone: customer.phone,
    approvers: customer.approvers.map((a: MockApproverRecord) => ({
      id: a.id,
      fullName: a.fullName,
      email: a.email,
      status: a.status,
    })),
  };
}

export function toWorkshopSummary(workshop: MockWorkshopRecord): WorkshopSummaryDto {
  return {
    id: workshop.id,
    name: workshop.name,
    taxId: workshop.taxId,
    city: workshop.city,
    status: workshop.status,
  };
}

export function toWorkshopDetail(workshop: MockWorkshopRecord): WorkshopDetailDto {
  return {
    ...toWorkshopSummary(workshop),
    email: workshop.email,
    phone: workshop.phone,
    address: workshop.address,
    createdAt: workshop.createdAt,
  };
}

export function toVehicleSummary(
  vehicle: MockVehicleRecord,
  relations: VehicleRelations
): VehicleSummaryDto {
  return {
    id: vehicle.id,
    plate: vehicle.plate,
    vin: vehicle.vin,
    brand: vehicle.brand,
    model: vehicle.model,
    modelYear: vehicle.modelYear,
    status: vehicle.status,
    statusChangedAt: vehicle.statusChangedAt,
    customerId: relations.customer.id,
    ownerName: relations.customer.name,
    customerName: relations.customer.name,
    workshopId: relations.workshop.id,
    workshopName: relations.workshop.name,
    responsibleApproverId: relations.responsible?.id ?? null,
    responsibleName: relations.responsible?.fullName ?? null,
    responsibleApproverName: relations.responsible?.fullName ?? null,
  };
}

export function toVehicleDetail(
  vehicle: MockVehicleRecord,
  relations: VehicleRelations
): VehicleDetailDto {
  const { customer, workshop, responsible } = relations;
  return {
    id: vehicle.id,
    plate: vehicle.plate,
    vin: vehicle.vin,
    brand: vehicle.brand,
    model: vehicle.model,
    modelYear: vehicle.modelYear,
    initialOdometerKm: vehicle.initialOdometerKm,
    status: vehicle.status,
    registeredAt: vehicle.registeredAt,
    statusChangedAt: vehicle.statusChangedAt,
    owner: { id: customer.id, name: customer.name, taxId: customer.taxId },
    workshop: { id: workshop.id, name: workshop.name },
    responsible: responsible
      ? { id: responsible.id, name: responsible.fullName, email: responsible.email }
      : null,
  };
}

export function toRoutineTypeDto(type: MockRoutineTypeRecord): RoutineTypeDto {
  return {
    id: type.id,
    code: type.code,
    name: type.name,
    defaultIntervalKm: type.defaultIntervalKm,
    defaultIntervalMonths: type.defaultIntervalMonths,
    dueSoonPercent: type.dueSoonPercent,
    status: type.status,
    updatedBy: type.updatedBy,
    updatedAt: type.updatedAt,
  };
}

export function toAlertEvent(alert: AlertDto): AlertChangedEvent {
  return {
    alertId: alert.alertId,
    vehicleId: alert.vehicleId,
    plate: alert.plate,
    customerId: alert.customerId,
    routineId: alert.routineId,
    routineName: alert.routineName,
    type: alert.type,
    triggeredBy: alert.triggeredBy,
    status: alert.status,
    raisedAt: alert.raisedAt,
    acknowledgedBy: alert.acknowledgedBy,
    resolutionReason: alert.resolutionReason,
    occurredAt: new Date().toISOString(),
  };
}
