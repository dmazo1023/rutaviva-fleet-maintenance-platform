export const HUB_ENDPOINT = '/hubs/notifications';

export const HubClientMethods = {
  joinCustomer: 'JoinCustomer',
  leaveCustomer: 'LeaveCustomer',
  joinApprover: 'JoinApprover',
  leaveApprover: 'LeaveApprover',
  joinWorkshop: 'JoinWorkshop',
  leaveWorkshop: 'LeaveWorkshop',
} as const;

export const HubServerMethods = {
  serviceOrderChanged: 'ServiceOrderChanged',
  approvalRequested: 'ApprovalRequested',
  alertChanged: 'AlertChanged',
  routineChanged: 'RoutineChanged',
  odometerReadingRecorded: 'OdometerReadingRecorded',
  vehicleChanged: 'VehicleChanged',
} as const;

export const HubMethods = {
  endpoint: HUB_ENDPOINT,
  client: HubClientMethods,
  server: HubServerMethods,
} as const;

