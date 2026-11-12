import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  Signal,
  signal,
  WritableSignal,
} from '@angular/core';
import { SessionStore } from '../../../../core/context/session.store';
import { ENUM_LABELS } from '../../../../core/models/enum-labels';
import { OdometerReadingIgnoredReason } from '../../../../core/models/enums';
import { ViewRefreshService } from '../../../../core/shell/view-refresh.service';
import { StatusPillComponent } from '../../../../shared/status-pill/status-pill.component';
import { SimulatorsApi } from '../../data/simulators.api';
import {
  OdometerSimulationResultVm,
  TimeEvaluationResultVm,
} from '../../models/simulators.vm';
import { currentDateInput, currentDateTimeLocal, toBogotaIso } from '../../simulators-datetime';
import { SIMULATORS_TEXTS } from '../../simulators.texts';

@Component({
  selector: 'app-simulators-page',
  imports: [StatusPillComponent],
  templateUrl: './simulators.page.html',
  styleUrl: './simulators.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SimulatorsPageComponent implements OnInit {
  private readonly api: SimulatorsApi = inject(SimulatorsApi);
  private readonly sessionStore: SessionStore = inject(SessionStore);
  private readonly viewRefresh: ViewRefreshService = inject(ViewRefreshService);

  readonly texts: typeof SIMULATORS_TEXTS = SIMULATORS_TEXTS;
  readonly labels: typeof ENUM_LABELS = ENUM_LABELS;

  readonly plateInput: WritableSignal<string> = signal<string>('RVA101');
  readonly kmInput: WritableSignal<string> = signal<string>('50200');
  readonly readAtInput: WritableSignal<string> = signal<string>(currentDateTimeLocal());
  readonly asOfDateInput: WritableSignal<string> = signal<string>(currentDateInput());

  readonly errorMessage: WritableSignal<string | null> = signal<string | null>(null);

  readonly odometerResult: WritableSignal<OdometerSimulationResultVm | null> =
    signal<OdometerSimulationResultVm | null>(null);
  readonly timeResult: WritableSignal<TimeEvaluationResultVm | null> =
    signal<TimeEvaluationResultVm | null>(null);

  readonly isOperator: Signal<boolean> = computed(
    () => this.sessionStore.profile() === 'Operator'
  );

  ngOnInit(): void {
    this.viewRefresh.setActiveVehicleId(null);
  }

  getIgnoredReasonLabel(reason: OdometerReadingIgnoredReason | null): string {
    if (!reason) {
      return '';
    }
    return this.labels.odometerReadingIgnoredReason[reason];
  }

  onPlateChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.plateInput.set(target.value);
  }

  onKmChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.kmInput.set(target.value);
  }

  onReadAtChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.readAtInput.set(target.value);
  }

  onAsOfDateChange(event: Event): void {
    const target: HTMLInputElement = event.target as HTMLInputElement;
    this.asOfDateInput.set(target.value);
  }

  onSubmitOdometer(event: Event): void {
    event.preventDefault();
    if (!this.isOperator()) {
      return;
    }
    const plate: string = this.plateInput().trim().toUpperCase();
    const odometerKm: number = Number(this.kmInput().trim());
    const readAt: string = toBogotaIso(this.readAtInput());

    this.api
      .recordOdometerReading({ plate, odometerKm, readAt })
      .subscribe({
        next: (res: OdometerSimulationResultVm) => this.onOdometerResult(res),
        error: () => this.onSubmitFailed(),
      });
  }

  onSubmitTimeEvaluation(event: Event): void {
    event.preventDefault();
    if (!this.isOperator()) {
      return;
    }
    const asOfDate: string = this.asOfDateInput().trim();
    this.api.runTimeEvaluation(asOfDate).subscribe({
      next: (res: TimeEvaluationResultVm) => this.onTimeResult(res),
      error: () => this.onSubmitFailed(),
    });
  }

  private onOdometerResult(res: OdometerSimulationResultVm): void {
    this.errorMessage.set(null);
    this.odometerResult.set(res);
    this.viewRefresh.requestRefresh();
  }

  private onTimeResult(res: TimeEvaluationResultVm): void {
    this.errorMessage.set(null);
    this.timeResult.set(res);
    this.viewRefresh.requestRefresh();
  }

  private onSubmitFailed(): void {
    this.errorMessage.set(this.texts.submitErrorMessage);
  }
}

