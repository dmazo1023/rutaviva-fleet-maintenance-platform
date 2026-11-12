import { HttpErrorResponse, HttpEvent, HttpResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { MockApiError } from './mock-backend.errors';

const HTTP_OK = 200;
const HTTP_ACCEPTED = 202;
const HTTP_NO_CONTENT = 204;

export interface ProblemDetailsBody {
  readonly title: string;
  readonly status: number;
  readonly detail: string;
  readonly code: string;
}

export function okResponse<T>(body: T): HttpResponse<T> {
  return new HttpResponse<T>({ status: HTTP_OK, body });
}

export function acceptedResponse<T>(body: T): HttpResponse<T> {
  return new HttpResponse<T>({ status: HTTP_ACCEPTED, body });
}

export function noContentResponse(): HttpResponse<null> {
  return new HttpResponse<null>({ status: HTTP_NO_CONTENT, body: null });
}

function toProblemBody(error: MockApiError): ProblemDetailsBody | null {
  if (error.code === '') {
    return null;
  }
  return { title: error.errorType, status: error.status, detail: error.detail, code: error.code };
}

export function toErrorResponse(error: MockApiError, url: string): HttpErrorResponse {
  return new HttpErrorResponse({
    status: error.status,
    statusText: error.errorType,
    url,
    error: toProblemBody(error),
  });
}

export function dispatch(
  url: string,
  handler: () => HttpResponse<unknown>
): Observable<HttpEvent<unknown>> {
  try {
    return of(handler());
  } catch (error: unknown) {
    if (error instanceof MockApiError) {
      return throwError(() => toErrorResponse(error, url));
    }
    throw error;
  }
}
