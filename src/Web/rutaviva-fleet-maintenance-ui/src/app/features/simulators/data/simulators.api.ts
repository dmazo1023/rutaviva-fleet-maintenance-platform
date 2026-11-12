import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { ApiRoutes } from '../../../core/api/api-routes';
import {
  OdometerReadingIgnoredReason,
  OdometerReadingOutcome,
} from '../../../core/models/enums';
import { OdometerSimulationResultVm, TimeEvaluationResultVm } from '../models/simulators.vm';

export interface RecordOdometerReadingRequest {
  readonly plate: string;
  readonly odometerKm: number;
  readonly readAt: string;
}

interface OdometerReadingResponse {
  readonly outcome: OdometerReadingOutcome;
  readonly reason: OdometerReadingIgnoredReason | null;
}

interface ReevaluationSummaryResponse {
  readonly evaluatedPlans: number;
  readonly dueSoon: number;
  readonly overdue: number;
  readonly ordersRequested: number;
}

@Injectable({
  providedIn: 'root',
})
export class SimulatorsApi {
  private readonly apiClient: ApiClient = inject(ApiClient);

  recordOdometerReading(
    body: RecordOdometerReadingRequest
  ): Observable<OdometerSimulationResultVm> {
    return this.apiClient
      .post<RecordOdometerReadingRequest, OdometerReadingResponse>(
        ApiRoutes.maintenance.odometerReadings(),
        body
      )
      .pipe(
        map(
          (response: OdometerReadingResponse): OdometerSimulationResultVm => ({
            plate: body.plate,
            odometerKm: body.odometerKm,
            readAt: body.readAt,
            outcome: response.outcome,
            ignoredReason: response.reason ?? null,
          })
        )
      );
  }

  runTimeEvaluation(asOfDate: string): Observable<TimeEvaluationResultVm> {
    return this.apiClient
      .post<{ readonly asOfDate: string }, ReevaluationSummaryResponse>(
        ApiRoutes.maintenance.timeEvaluations(),
        { asOfDate }
      )
      .pipe(
        map(
          (summary: ReevaluationSummaryResponse): TimeEvaluationResultVm => ({
            asOfDate,
            evaluatedPlans: summary.evaluatedPlans,
            dueSoon: summary.dueSoon,
            overdue: summary.overdue,
            ordersRequested: summary.ordersRequested,
          })
        )
      );
  }
}
