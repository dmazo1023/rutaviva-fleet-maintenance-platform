export interface AppEnvironment {
  readonly production: boolean;
  readonly apiBaseUrl: string;
  readonly useMockBackend: boolean;
  readonly realtimeEnabled: boolean;
}

export const environment: AppEnvironment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000',
  useMockBackend: false,
  realtimeEnabled: false,
};
