import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  FleetApi,
  mapVehicleDetailDtoToVm,
  mapVehicleSummaryDtoToVm,
  VehicleDetailDto,
  VehicleSummaryDto,
  VEHICLES_PAGE_SIZE,
} from './fleet.api';
import { VehicleDetailVm, VehicleSummaryVm } from '../models/fleet.vm';

const VEHICLE_ID = '0b1f6a52-1b9e-4c6e-9d0e-0e6a2f6b7c11';
const PLATE = 'RVA101';

const summaryDto: VehicleSummaryDto = {
  id: VEHICLE_ID,
  plate: PLATE,
  vin: null,
  brand: 'Renault',
  model: 'Kangoo',
  modelYear: 2021,
  status: 'Active',
  customerId: 'cust-1',
  ownerName: 'RutaViva S.A.S.',
  workshopId: 'ws-1',
  workshopName: 'Autofrenos Medellín',
  responsibleApproverId: null,
  responsibleName: null,
};

const detailDto: VehicleDetailDto = {
  id: VEHICLE_ID,
  plate: PLATE,
  vin: '9FB4SRC9AKM100101',
  brand: 'Renault',
  model: 'Kangoo',
  modelYear: 2021,
  initialOdometerKm: 52_300,
  status: 'Active',
  owner: { id: 'cust-1', name: 'RutaViva S.A.S.', taxId: '900000001' },
  workshop: { id: 'ws-1', name: 'Autofrenos Medellín' },
  responsible: { id: 'app-1', name: 'Laura Gómez', email: 'laura@rutaviva.co' },
};

describe('Fleet DTO mappers', () => {
  it('should map the summary DTO using ownerName and responsibleName', () => {
    const vm: VehicleSummaryVm = mapVehicleSummaryDtoToVm(summaryDto);

    expect(vm.vehicleId).toBe(VEHICLE_ID);
    expect(vm.customerName).toBe('RutaViva S.A.S.');
    expect(vm.responsibleApproverName).toBeNull();
  });

  it('should map the nested detail DTO into a flat VM without invented fields', () => {
    const vm: VehicleDetailVm = mapVehicleDetailDtoToVm(detailDto);

    expect(vm.customerId).toBe('cust-1');
    expect(vm.workshopName).toBe('Autofrenos Medellín');
    expect(vm.responsibleApproverName).toBe('Laura Gómez');
    expect(vm.initialOdometerKm).toBe(52_300);
    expect('lastReadingText' in vm).toBe(false);
  });

  it('should map a missing responsible to null', () => {
    const vm: VehicleDetailVm = mapVehicleDetailDtoToVm({ ...detailDto, responsible: null });

    expect(vm.responsibleApproverId).toBeNull();
    expect(vm.responsibleApproverName).toBeNull();
  });
});

describe('FleetApi', () => {
  let api: FleetApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(FleetApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should read the paged result items requesting the maximum page size', () => {
    let result: readonly VehicleSummaryVm[] = [];
    api.getVehicles({ customerId: 'cust-1' }).subscribe((value) => (result = value));

    const request = http.expectOne(
      `/api/v1/fleet/vehicles?customerId=cust-1&pageSize=${VEHICLES_PAGE_SIZE}`
    );
    request.flush({ items: [summaryDto], page: 1, pageSize: VEHICLES_PAGE_SIZE, totalCount: 1 });

    expect(result.length).toBe(1);
    expect(result[0]?.plate).toBe(PLATE);
  });

  it('should return null when the plate does not exist (404)', () => {
    let result: VehicleDetailVm | null | undefined;
    api.getVehicleByPlate('ZZZ999').subscribe((value) => (result = value));

    http
      .expectOne('/api/v1/fleet/vehicles/by-plate/ZZZ999')
      .flush(null, { status: 404, statusText: 'Not Found' });

    expect(result).toBeNull();
  });

  it('should propagate non-404 errors when reading by plate', () => {
    let error: unknown;
    api.getVehicleByPlate(PLATE).subscribe({ error: (e: unknown) => (error = e) });

    http
      .expectOne(`/api/v1/fleet/vehicles/by-plate/${PLATE}`)
      .flush(null, { status: 500, statusText: 'Server Error' });

    expect(error instanceof HttpErrorResponse).toBe(true);
  });

  it('should reload the vehicle by plate after a 204 status change', () => {
    let result: VehicleDetailVm | null | undefined;
    api.updateVehicleStatus(VEHICLE_ID, PLATE, 'Inactive').subscribe((value) => (result = value));

    const patch = http.expectOne(`/api/v1/fleet/vehicles/${VEHICLE_ID}/status`);
    expect(patch.request.method).toBe('PATCH');
    patch.flush(null, { status: 204, statusText: 'No Content' });

    http
      .expectOne(`/api/v1/fleet/vehicles/by-plate/${PLATE}`)
      .flush({ ...detailDto, status: 'Inactive' });

    expect(result?.status).toBe('Inactive');
  });
});
