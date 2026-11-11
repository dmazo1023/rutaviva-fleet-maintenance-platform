export type LoadableStatus = 'idle' | 'loading' | 'ready' | 'error';

export interface Loadable<T> {
  readonly status: LoadableStatus;
  readonly data: T;
  readonly error: string | null;
}

export function createIdleLoadable<T>(initialData: T): Loadable<T> {
  return {
    status: 'idle',
    data: initialData,
    error: null,
  };
}

export function createLoadingLoadable<T>(currentData: T): Loadable<T> {
  return {
    status: 'loading',
    data: currentData,
    error: null,
  };
}

export function createReadyLoadable<T>(data: T): Loadable<T> {
  return {
    status: 'ready',
    data,
    error: null,
  };
}

export function createErrorLoadable<T>(currentData: T, error: string): Loadable<T> {
  return {
    status: 'error',
    data: currentData,
    error,
  };
}

