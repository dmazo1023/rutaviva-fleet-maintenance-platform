import {
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
} from '../../../core/models/enums';

export interface OdometerSimulationResultVm {
  readonly plate: string;
  readonly odometerKm: number;
  readonly readAt: string;
  readonly outcome: OdometerReadingOutcome;
  readonly ignoredReason: OdometerReadingIgnoredReason | null;
}

export interface TimeEvaluationResultVm {
  readonly asOfDate: string;
  readonly evaluatedPlans: number;
  readonly dueSoon: number;
  readonly overdue: number;
  readonly ordersRequested: number;
}
