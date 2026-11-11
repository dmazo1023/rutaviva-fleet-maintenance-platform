import { DueStatus } from '../../core/models/enums';

export type RoutineTrackKind = 'km' | 'time';

export interface RoutineTrackVm {
  readonly kind: RoutineTrackKind;
  readonly consumedPercent: number;
  readonly dueSoonPercent: number;
  readonly remaining: number;
  readonly status: DueStatus;
}

