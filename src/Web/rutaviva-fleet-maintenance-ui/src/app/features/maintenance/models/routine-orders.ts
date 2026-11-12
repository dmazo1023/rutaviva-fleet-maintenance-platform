import { ServiceOrderStatus } from '../../../core/models/enums';
import { PlanOrderSummaryVm, RoutineVm } from './maintenance.vm';

const CLOSED_ORDER_STATUSES: ReadonlySet<ServiceOrderStatus> = new Set<ServiceOrderStatus>([
  'Completed',
  'Cancelled',
]);

export function isOpenOrder(order: PlanOrderSummaryVm): boolean {
  return !CLOSED_ORDER_STATUSES.has(order.status);
}

function withOpenOrder(routine: RoutineVm, order: PlanOrderSummaryVm | undefined): RoutineVm {
  return {
    ...routine,
    openServiceOrderId: order?.id ?? null,
    openServiceOrderNumber: order?.number ?? null,
    openServiceOrderStatus: order?.status ?? null,
  };
}

export function withOpenOrders(
  routines: readonly RoutineVm[],
  orders: readonly PlanOrderSummaryVm[]
): readonly RoutineVm[] {
  const openByRoutine: ReadonlyMap<string, PlanOrderSummaryVm> = new Map<string, PlanOrderSummaryVm>(
    orders.filter(isOpenOrder).map((order: PlanOrderSummaryVm) => [order.routineId, order])
  );
  return routines.map((routine: RoutineVm) =>
    withOpenOrder(routine, openByRoutine.get(routine.routineId))
  );
}
