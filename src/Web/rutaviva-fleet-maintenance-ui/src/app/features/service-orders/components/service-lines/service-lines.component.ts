import {
  ChangeDetectionStrategy,
  Component,
  input,
  InputSignal,
  output,
  OutputEmitterRef,
  signal,
  WritableSignal,
} from '@angular/core';
import { ENUM_LABELS } from '../../../../core/models/enum-labels';
import { ServiceOrderStatus } from '../../../../core/models/enums';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { ServiceLineVm } from '../../models/service-orders.vm';
import { SERVICE_ORDERS_TEXTS } from '../../service-orders.texts';

export interface PriceLineEvent {
  readonly lineId: string;
  readonly amount: number;
}

const COP_FORMATTER: Intl.NumberFormat = new Intl.NumberFormat('es-CO');

@Component({
  selector: 'app-service-lines',
  imports: [StatusPillComponent],
  templateUrl: './service-lines.component.html',
  styleUrl: './service-lines.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceLinesComponent {
  readonly lines: InputSignal<readonly ServiceLineVm[]> =
    input.required<readonly ServiceLineVm[]>();
  readonly total: InputSignal<number> = input.required<number>();
  readonly status: InputSignal<ServiceOrderStatus> = input.required<ServiceOrderStatus>();
  readonly autoApproved: InputSignal<boolean> = input<boolean>(false);
  readonly autoApprovalMaxAmount: InputSignal<number> = input<number>(0);
  readonly isWorkshop: InputSignal<boolean> = input<boolean>(false);

  readonly priceSaved: OutputEmitterRef<PriceLineEvent> = output<PriceLineEvent>();

  readonly texts: typeof SERVICE_ORDERS_TEXTS = SERVICE_ORDERS_TEXTS;
  readonly labels = ENUM_LABELS;
  readonly draftPrices: WritableSignal<Readonly<Record<string, string>>> = signal<
    Readonly<Record<string, string>>
  >({});

  formatCop(amount: number): string {
    return `$ ${COP_FORMATTER.format(amount)}`;
  }

  getDraftPrice(lineId: string): string {
    return this.draftPrices()[lineId] ?? '';
  }

  onPriceInput(lineId: string, event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.draftPrices.update((prev: Readonly<Record<string, string>>) => ({
      ...prev,
      [lineId]: target.value,
    }));
  }

  onSavePrice(lineId: string): void {
    const raw: string = this.getDraftPrice(lineId).trim();
    const parsed: number = Number(raw);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      return;
    }
    this.priceSaved.emit({ lineId, amount: parsed });
  }
}

