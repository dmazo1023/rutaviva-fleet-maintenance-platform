import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastStore } from '../feedback/toast.store';
import { ApiClient, DownloadedFile } from './api-client';
import {
  OrderConflictEventsService,
  problemDetailsInterceptor,
} from './problem-details.interceptor';

describe('ApiClient and problemDetailsInterceptor', () => {
  let apiClient: ApiClient;
  let httpMock: HttpTestingController;
  let toastStore: ToastStore;
  let conflictEvents: OrderConflictEventsService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    apiClient = TestBed.inject(ApiClient);
    httpMock = TestBed.inject(HttpTestingController);
    toastStore = TestBed.inject(ToastStore);
    conflictEvents = TestBed.inject(OrderConflictEventsService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should extract fileName from Content-Disposition header on download', () => {
    let downloaded: DownloadedFile | undefined;

    apiClient.download('/api/v1/reports/ServiceOrders/download?format=xlsx').subscribe((res) => {
      downloaded = res;
    });

    const req = httpMock.expectOne('/api/v1/reports/ServiceOrders/download?format=xlsx');
    expect(req.request.method).toBe('GET');
    req.flush(new Blob(['contenido']), {
      headers: {
        'Content-Disposition': 'attachment; filename="ordenes-2026.xlsx"',
      },
    });

    expect(downloaded?.fileName).toBe('ordenes-2026.xlsx');
    expect(downloaded?.blob.size).toBeGreaterThan(0);
  });

  it('should display ProblemDetails toast on 422 business rule error', () => {
    let failed = false;

    apiClient.post('/api/v1/service-orders/OS-0001/approve', {}).subscribe({
      error: () => {
        failed = true;
      },
    });

    const req = httpMock.expectOne('/api/v1/service-orders/OS-0001/approve');
    req.flush(
      {
        title: 'Transición no permitida',
        detail: 'La orden tiene líneas pendientes de precio.',
        code: 'ServiceOrder.InvalidTransition',
      },
      { status: 422, statusText: 'Unprocessable Entity' }
    );

    expect(failed).toBe(true);
    expect(toastStore.toasts().length).toBe(1);
    expect(toastStore.toasts()[0]?.message).toBe('Transición no permitida');
    expect(toastStore.toasts()[0]?.detail).toBe('La orden tiene líneas pendientes de precio.');
  });

  it('should show toast and emit reload event on 409 conflict for a service order', () => {
    const conflicts: string[] = [];
    conflictEvents.orderConflict$.subscribe((id: string) => conflicts.push(id));

    apiClient.post('/api/v1/service-orders/OS-0004/approve', {}).subscribe({
      error: () => undefined,
    });

    const req = httpMock.expectOne('/api/v1/service-orders/OS-0004/approve');
    req.flush(
      {
        title: 'Conflicto de concurrencia',
        detail: 'La orden cambió mientras la revisabas.',
        code: 'Concurrency.Conflict',
      },
      { status: 409, statusText: 'Conflict' }
    );

    expect(toastStore.toasts().length).toBe(1);
    expect(toastStore.toasts()[0]?.message).toBe('Conflicto de concurrencia');
    expect(conflicts).toEqual(['OS-0004']);
  });
});

