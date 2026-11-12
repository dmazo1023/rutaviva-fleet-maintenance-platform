import { HttpEvent, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { dispatch } from './mock-backend.http';
import { MOCK_ROUTES, MockRoute } from './mock-backend.routes';
import { MockBackendState } from './mock-backend.state';

const LOCAL_ORIGIN = 'http://localhost';
const ANY_METHOD = '*';

function asRecord(body: unknown): Record<string, unknown> {
  if (typeof body === 'object' && body !== null) {
    return body as Record<string, unknown>;
  }
  return {};
}

function decodeCaptures(match: RegExpExecArray): string[] {
  return match.slice(1).map((value: string | undefined) => decodeURIComponent(value ?? ''));
}

function findRoute(method: string, path: string): { route: MockRoute; match: RegExpExecArray } | null {
  for (const route of MOCK_ROUTES) {
    const match: RegExpExecArray | null = route.pattern.exec(path);
    if (match && (route.method === method || route.method === ANY_METHOD)) {
      return { route, match };
    }
  }
  return null;
}

export const mockBackendInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: (request: HttpRequest<unknown>) => Observable<HttpEvent<unknown>>
): Observable<HttpEvent<unknown>> => {
  if (!environment.useMockBackend) {
    return next(req);
  }

  const url: URL = new URL(req.url, LOCAL_ORIGIN);
  const found: { route: MockRoute; match: RegExpExecArray } | null = findRoute(
    req.method.toUpperCase(),
    url.pathname
  );
  if (!found) {
    return next(req);
  }

  const state: MockBackendState = inject(MockBackendState);
  const body: Record<string, unknown> = asRecord(req.body);
  return dispatch(req.url, () =>
    found.route.handle({ state, captures: decodeCaptures(found.match), params: url.searchParams, body })
  );
};
