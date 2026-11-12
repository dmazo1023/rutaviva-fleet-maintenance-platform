import { ApiRoutes } from './api-routes';

describe('ApiRoutes', () => {
  it('should build fleet vehicles URL with and without query parameters', () => {
    expect(ApiRoutes.fleet.vehicles()).toBe('/api/v1/fleet/vehicles');
    expect(ApiRoutes.fleet.vehicles({ customerId: 'cust-1', status: 'Active' })).toBe(
      '/api/v1/fleet/vehicles?customerId=cust-1&status=Active'
    );
  });

  it('should build maintenance plans and routine parameters URLs', () => {
    expect(ApiRoutes.maintenance.plans()).toBe('/api/v1/maintenance/plans');
    expect(ApiRoutes.maintenance.plans('900000001')).toBe(
      '/api/v1/maintenance/plans?customerId=900000001'
    );
    expect(ApiRoutes.maintenance.routineById('veh-1', 'rt-1')).toBe(
      '/api/v1/maintenance/plans/veh-1/routines/rt-1'
    );
  });

  it('should build alerts URL with optional customer and status filters', () => {
    expect(ApiRoutes.maintenance.alerts()).toBe('/api/v1/maintenance/alerts');
    expect(ApiRoutes.maintenance.alerts({ customerId: '900000001', status: 'Active' })).toBe(
      '/api/v1/maintenance/alerts?customerId=900000001&status=Active'
    );
  });

  it('should build service orders detail URL with actor context', () => {
    expect(ApiRoutes.serviceOrders.byId('OS-0001')).toBe('/api/v1/service-orders/OS-0001');
    expect(
      ApiRoutes.serviceOrders.byId('OS-0001', { actor: 'Approver', actorId: 'app-1' })
    ).toBe('/api/v1/service-orders/OS-0001?actor=Approver&actorId=app-1');
  });

  it('should build report download URL with format and filters', () => {
    expect(
      ApiRoutes.reports.download('ServiceOrders', 'Xlsx', {
        customerId: '900000001',
        status: 'Approved',
      })
    ).toBe('/api/v1/reports/ServiceOrders/download?format=xlsx&customerId=900000001&status=Approved');
  });
});

