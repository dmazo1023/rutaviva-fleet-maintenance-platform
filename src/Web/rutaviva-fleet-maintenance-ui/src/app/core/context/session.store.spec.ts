import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HUB_CONNECTION_FACTORY } from '../realtime/notifications.service';
import { BROWSER_STORAGE, SESSION_STORAGE_KEY, SessionStore, StorageLike } from './session.store';

const CUSTOMER_ID = '3f2b8c1e-0000-4000-8000-000000000001';
const APPROVER_ID = '3f2b8c1e-0000-4000-8000-0000000000a1';

const CUSTOMER_SUMMARY = {
  id: CUSTOMER_ID,
  taxId: '900000001',
  name: 'RutaViva S.A.S.',
  status: 'Active',
};

const CUSTOMER_DETAIL = {
  ...CUSTOMER_SUMMARY,
  approvers: [
    {
      id: APPROVER_ID,
      fullName: 'Laura Gómez',
      email: 'laura@rutaviva.com',
      status: 'Active',
    },
  ],
};

function flushPromises(): Promise<void> {
  return new Promise<void>((resolve) => setTimeout(resolve));
}

function createStorage(initial: string | null): StorageLike {
  let value: string | null = initial;
  return {
    getItem: (): string | null => value,
    setItem: (_key: string, next: string): void => {
      value = next;
    },
  };
}

describe('SessionStore', () => {
  let httpMock: HttpTestingController;

  function configure(storage: StorageLike | null): SessionStore {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BROWSER_STORAGE, useValue: storage },
        { provide: HUB_CONNECTION_FACTORY, useValue: () => null },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    return TestBed.inject(SessionStore);
  }

  async function loadCatalogs(store: SessionStore): Promise<void> {
    const loading: Promise<void> = store.loadContextCatalogs();
    httpMock.expectOne('/api/v1/customers').flush([CUSTOMER_SUMMARY]);
    httpMock.expectOne('/api/v1/workshops?status=Active').flush([]);
    await flushPromises();
    httpMock
      .match((request) => request.url.startsWith('/api/v1/customers/'))
      .forEach((request) => request.flush({ ...CUSTOMER_DETAIL, approvers: [] }));
    await flushPromises();
    await loading;
  }

  afterEach(() => {
    httpMock.verify();
  });

  it('should not throw when localStorage throws on getItem and setItem', async () => {
    const throwingStorage: StorageLike = {
      getItem(): string | null {
        throw new Error('Storage disabled');
      },
      setItem(): void {
        throw new Error('QuotaExceededError');
      },
    };
    const store: SessionStore = configure(throwingStorage);

    expect(store.profile()).toBe('Operator');
    expect(() => store.setProfile('Approver')).not.toThrow();
    await loadCatalogs(store);
    expect(store.profile()).toBe('Approver');
  });

  it('should default approvers to an empty list and keep the Approver profile usable', async () => {
    const store: SessionStore = configure(createStorage(null));
    await loadCatalogs(store);

    store.setProfile('Approver');

    expect(store.customers()[0]?.approvers).toEqual([]);
    expect(store.actorId()).toBeNull();
    expect(store.currentActorName()).toBe('Aprobador');
  });

  it('should load customer detail approvers when a customer is selected', async () => {
    const store: SessionStore = configure(createStorage(null));
    await loadCatalogs(store);
    store.setProfile('Approver');

    store.setCustomer(CUSTOMER_ID);
    httpMock.expectOne(`/api/v1/customers/${CUSTOMER_ID}`).flush(CUSTOMER_DETAIL);
    await flushPromises();

    expect(store.currentCustomer()?.approvers[0]?.customerId).toBe(CUSTOMER_ID);
    expect(store.actorId()).toBe(APPROVER_ID);
    expect(store.currentActorName()).toBe('Laura Gómez');
  });

  it('should discard a stored customer that is not among the loaded customers', async () => {
    const persisted: string = JSON.stringify({
      customerId: '900000001',
      profile: 'Operator',
      actorId: null,
    });
    const store: SessionStore = configure(createStorage(persisted));
    expect(store.customerId()).toBe('900000001');

    await loadCatalogs(store);

    expect(store.customerId()).toBe('');
  });

  it('should use a versioned storage key that ignores legacy sessions', () => {
    expect(SESSION_STORAGE_KEY).toBe('rutaviva.crm.session.v2');
  });
});
