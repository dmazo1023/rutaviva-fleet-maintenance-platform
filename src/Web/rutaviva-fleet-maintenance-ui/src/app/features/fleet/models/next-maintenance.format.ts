import { DueStatus } from '../../../core/models/enums';
import { NextMaintenanceVm } from '../../maintenance/models/maintenance.vm';
import { FLEET_TEXTS } from '../fleet.texts';

const NUMBER_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');
const NON_BREAKING_SPACE = '\u00A0';
const MINUS_SIGN = '\u2212';
const TONE_BY_STATUS: Readonly<Record<DueStatus, string>> = {
  Overdue: 'is-danger',
  DueSoon: 'is-warn',
  UpToDate: 'is-ok',
};

export function toneOfDueStatus(status: DueStatus): string {
  return TONE_BY_STATUS[status];
}

export function formatRemaining(next: NextMaintenanceVm): string {
  const sign: string = next.remaining < 0 ? MINUS_SIGN : '';
  const magnitude: string = NUMBER_FORMATTER.format(Math.abs(next.remaining));
  const unit: string = next.kind === 'km' ? FLEET_TEXTS.kmUnit : FLEET_TEXTS.daysUnit;
  return `${sign}${magnitude}${NON_BREAKING_SPACE}${unit}`;
}
