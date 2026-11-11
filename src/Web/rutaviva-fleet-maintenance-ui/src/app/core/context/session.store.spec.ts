import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HUB_CONNECTION_FACTORY } from '../realtime/notifications.service';
import { BROWSER_STORAGE, SessionStore, StorageLike } from './session.store';

describe('SessionStore', () => {
  it('should not throw when localStorage throws on getItem and setItem', async () => {
    const throwingStorage: StorageLike = {
      getItem(): string | null {
        throw new Error('Storage disabled');
      },
      setItem(): void {
        throw new Error('QuotaExceededError');
      },
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BROWSER_STORAGE, useValue: throwingStorage },
        { provide: HUB_CONNECTION_FACTORY, useValue: () => null },
      ],
    });

    const httpMock: HttpTestingController = TestBed.inject(HttpTestingController);
    const store: SessionStore = TestBed.inject(SessionStore);

    expect(store.profile()).toBe('Operator');
    expect(() => store.setCustomer('900000001')).not.toThrow();
    expect(store.customerId()).toBe('900000001');
    expect(() => store.setProfile('Approver')).not.toThrow();
    expect(store.profile()).toBe('Approver');

    const loadPromise: Promise<void> = store.loadContextCatalogs();
    const custReq = httpMock.expectOne('/api/v1/customers');
    custReq.flush([
      {
        id: '900000001',
        taxId: '900000001',
        name: 'RutaViva S.A.S.',
        status: 'Active',
        approvers: [
          {
            id: 'Laura_Gomez',
            customerId: '900000001',
            fullName: 'Laura Gómez',
            email: 'Laura_Gomez@rutaviva.com',
            status: 'Active',
          },
        ],
      },
    ]);

    const shopReq = httpMock.expectOne('/api/v1/workshops?status=Active');
    shopReq.flush([
      {
        id: '900100001',
        taxId: '900100001',
        name: 'Autofrenos Medellín',
        city: 'Medellín',
        status: 'Active',
      },
    ]);

    await loadPromise;
    expect(store.actorId()).toBe('Laura_Gomez');
    expect(store.currentActorName()).toBe('Laura Gómez');
    httpMock.verify();
  });
});

