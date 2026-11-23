import { Injectable, signal } from '@angular/core';

export interface ApiErrorNotice {
  message: string;
  traceId?: string;
}

@Injectable({ providedIn: 'root' })
export class HttpErrorService {
  readonly notice = signal<ApiErrorNotice | null>(null);

  report(notice: ApiErrorNotice): void {
    this.notice.set(notice);
  }

  clear(): void {
    this.notice.set(null);
  }
}
