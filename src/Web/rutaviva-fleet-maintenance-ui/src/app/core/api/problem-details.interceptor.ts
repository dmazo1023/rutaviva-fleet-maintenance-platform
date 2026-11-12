import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, Observable, Subject, throwError } from 'rxjs';
import { ToastStore } from '../feedback/toast.store';
import {
  API_ERROR_FALLBACK_MESSAGE,
  API_ERROR_MESSAGES,
  API_ERROR_TITLE,
} from './api-errors.texts';

export interface ProblemDetailsPayload {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
  readonly code?: string;
}

const HANDLED_STATUS_CODES: readonly number[] = [400, 409, 422];
const HTTP_CONFLICT_STATUS = 409;
const SERVICE_ORDER_ID_REGEX = /\/api\/v1\/service-orders\/([^/?]+)/;

@Injectable({
  providedIn: 'root',
})
export class OrderConflictEventsService {
  private readonly conflictOrderIdSubject = new Subject<string>();
  readonly orderConflict$: Observable<string> = this.conflictOrderIdSubject.asObservable();

  notifyConflict(orderId: string): void {
    this.conflictOrderIdSubject.next(orderId);
  }
}

function extractProblemDetails(payload: unknown): ProblemDetailsPayload | null {
  if (typeof payload !== 'object' || payload === null) {
    return null;
  }
  const record: Record<string, unknown> = payload as Record<string, unknown>;
  return {
    title: typeof record['title'] === 'string' ? record['title'] : undefined,
    detail: typeof record['detail'] === 'string' ? record['detail'] : undefined,
    status: typeof record['status'] === 'number' ? record['status'] : undefined,
    code: typeof record['code'] === 'string' ? record['code'] : undefined,
  };
}

export function resolveErrorMessage(problem: ProblemDetailsPayload | null): string {
  const code: string | undefined = problem?.code;
  return (code && API_ERROR_MESSAGES[code]) || API_ERROR_FALLBACK_MESSAGE;
}

function extractServiceOrderId(url: string | null): string | null {
  if (!url) {
    return null;
  }
  const match: RegExpExecArray | null = SERVICE_ORDER_ID_REGEX.exec(url);
  if (!match?.[1] || match[1] === 'approval-policies' || match[1] === 'approvals') {
    return null;
  }
  return decodeURIComponent(match[1]);
}

export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) => {
  const toastStore: ToastStore = inject(ToastStore);
  const conflictEvents: OrderConflictEventsService = inject(OrderConflictEventsService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && HANDLED_STATUS_CODES.includes(error.status)) {
        const problem: ProblemDetailsPayload | null = extractProblemDetails(error.error);

        toastStore.show({
          message: API_ERROR_TITLE,
          detail: resolveErrorMessage(problem),
          tone: 'danger',
          isError: true,
        });

        if (error.status === HTTP_CONFLICT_STATUS) {
          const orderId: string | null = extractServiceOrderId(error.url ?? req.url);
          if (orderId) {
            conflictEvents.notifyConflict(orderId);
          }
        }
      }

      return throwError(() => error);
    })
  );
};

