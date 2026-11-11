import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { ApiRoutes } from '../../../core/api/api-routes';
import {
  OdometerSimulationResultVm,
  TimeEvaluationResultVm,
} from '../models/simulators.vm';

export interface RecordOdometerReadingRequest {
  readonly plate: string;
  readonly kilometers: number;
  readonly readAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class SimulatorsApi {
  private readonly apiClient: ApiClient = inject(ApiClient);

  recordOdometerReading(
    body: RecordOdometerReadingRequest
  ): Observable<OdometerSimulationResultVm> {
    return this.apiClient.post<RecordOdometerReadingRequest, OdometerSimulationResultVm>(
      ApiRoutes.maintenance.odometerReadings(),
      body
    );
  }

  runTimeEvaluation(asOfDate: string): Observable<TimeEvaluationResultVm> {
    return this.apiClient.post<{ readonly asOfDate: string }, TimeEvaluationResultVm>(
      ApiRoutes.maintenance.timeEvaluations(),
      { asOfDate }
    );
  }
}

