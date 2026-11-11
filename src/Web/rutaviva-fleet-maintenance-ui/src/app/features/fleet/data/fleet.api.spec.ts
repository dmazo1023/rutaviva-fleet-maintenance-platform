import {
  mapOdometerReadingDtoToVm,
  mapPlanSummaryDtoToVm,
  mapRoutineDtoToVm,
} from '../../maintenance/data/maintenance.api';
import { mapVehicleDtoToDetailVm } from './fleet.api';

describe('FleetApi and MaintenanceApi DTO to VM mappers', () => {
  it('should map VehicleDto and MaintenancePlanSummaryDto to immutable VMs', () => {
    const vehicleVm = mapVehicleDtoToDetailVm({
      id: 'RVA101',
      plate: 'RVA101',
      vin: '9FB4SRC9AKM100101',
      brand: 'Renault',
      model: 'Kangoo',
      modelYear: 2021,
      customerId: '900000001',
      customerName: 'RutaViva S.A.S.',
      workshopId: '900100001',
      workshopName: 'Autofrenos Medellín',
      responsibleApproverId: 'Laura_Gomez',
      responsibleApproverName: 'Laura Gómez',
      status: 'Active',
      odometerKm: 52_300,
    });

    expect(vehicleVm.vehicleId).toBe('RVA101');
    expect(vehicleVm.lastReadingText).toBe('hace 3 min');

    const summaryVm = mapPlanSummaryDtoToVm({
      vehicleId: 'RVA102',
      plate: 'RVA102',
      customerId: '900000001',
      status: 'Active',
      odometerKm: 81_450,
      worstDueStatus: 'DueSoon',
      worstConsumedPercent: 94.5,
      worstTriggeredBy: 'Distance',
      nextRoutineName: 'Cambio de aceite',
      nextRemainingKind: 'km',
      nextRemainingValue: 550,
    });

    expect(summaryVm.worstDueStatus).toBe('DueSoon');
    expect(summaryVm.worstConsumedPercent).toBe(94.5);
  });

  it('should map RoutineDto and OdometerReadingDto to VMs with tracks and ignoredReason', () => {
    const routineVm = mapRoutineDtoToVm({
      routineId: 'EVA30D-OIL_CHANGE',
      vehicleId: 'EVA30D',
      plate: 'EVA30D',
      name: 'Cambio de aceite',
      typeCode: 'OIL_CHANGE',
      state: 'Active',
      dueStatus: 'DueSoon',
      triggeredBy: 'Distance',
      tracks: [
        {
          kind: 'km',
          consumedPercent: 83.33,
          dueSoonPercent: 20,
          remaining: 500,
          status: 'DueSoon',
        },
      ],
      nextDueKm: 13_000,
      nextDueDate: '2027-01-01',
      effectiveDueSoonPercent: 20,
      thresholdSource: 'Routine',
      openServiceOrderId: null,
      canCreateOrder: false,
    });

    expect(routineVm.icon).toBe('oil_barrel');
    expect(routineVm.thresholdSource).toBe('Routine');
    expect(routineVm.tracks[0]?.dueSoonPercent).toBe(20);

    const readingVm = mapOdometerReadingDtoToVm({
      kilometers: 53_000,
      readAt: '12/11/2026 08:41',
      outcome: 'Ignored',
      ignoredReason: 'NotGreaterThanCurrent',
    });

    expect(readingVm.outcome).toBe('Ignored');
    expect(readingVm.ignoredReason).toBe('NotGreaterThanCurrent');
  });
});

