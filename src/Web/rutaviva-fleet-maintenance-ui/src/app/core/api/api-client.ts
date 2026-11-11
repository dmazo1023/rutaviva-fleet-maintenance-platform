import { HttpClient, HttpResponse } from '@angular/common/http';
import { inject, Injectable, InjectionToken } from '@angular/core';
import { map, Observable } from 'rxjs';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: (): string => '',
});

export interface DownloadedFile {
  readonly blob: Blob;
  readonly fileName: string | null;
}

const CONTENT_DISPOSITION_HEADER = 'Content-Disposition';
const FILENAME_UTF8_REGEX = /filename\*=UTF-8''([^;\n]+)/i;
const FILENAME_PLAIN_REGEX = /filename="?([^";\n]+)"?/i;

export function extractFileNameFromContentDisposition(headerValue: string | null): string | null {
  if (!headerValue) {
    return null;
  }

  const utf8Match: RegExpExecArray | null = FILENAME_UTF8_REGEX.exec(headerValue);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1].trim());
  }

  const plainMatch: RegExpExecArray | null = FILENAME_PLAIN_REGEX.exec(headerValue);
  if (plainMatch?.[1]) {
    return plainMatch[1].trim();
  }

  return null;
}

@Injectable({
  providedIn: 'root',
})
export class ApiClient {
  private readonly http: HttpClient = inject(HttpClient);
  private readonly baseUrl: string = inject(API_BASE_URL);

  get<TResponse>(path: string): Observable<TResponse> {
    return this.http.get<TResponse>(this.resolveUrl(path));
  }

  post<TRequest, TResponse>(path: string, body: TRequest): Observable<TResponse> {
    return this.http.post<TResponse>(this.resolveUrl(path), body);
  }

  put<TRequest, TResponse>(path: string, body: TRequest): Observable<TResponse> {
    return this.http.put<TResponse>(this.resolveUrl(path), body);
  }

  patch<TRequest, TResponse>(path: string, body: TRequest): Observable<TResponse> {
    return this.http.patch<TResponse>(this.resolveUrl(path), body);
  }

  download(path: string): Observable<DownloadedFile> {
    return this.http
      .get(this.resolveUrl(path), {
        observe: 'response',
        responseType: 'blob',
      })
      .pipe(
        map((response: HttpResponse<Blob>): DownloadedFile => {
          const disposition: string | null = response.headers.get(CONTENT_DISPOSITION_HEADER);
          return {
            blob: response.body ?? new Blob([]),
            fileName: extractFileNameFromContentDisposition(disposition),
          };
        })
      );
  }

  private resolveUrl(path: string): string {
    if (!this.baseUrl || path.startsWith('http://') || path.startsWith('https://')) {
      return path;
    }
    const normalizedBase: string = this.baseUrl.endsWith('/')
      ? this.baseUrl.slice(0, -1)
      : this.baseUrl;
    const normalizedPath: string = path.startsWith('/') ? path : `/${path}`;
    return `${normalizedBase}${normalizedPath}`;
  }
}

