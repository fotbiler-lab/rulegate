import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AppSettings } from './app-settings';

export interface DocumentRecord {
  readonly id: number;
  readonly title: string;
  readonly ownerUsername: string;
  readonly organizationId: string;
  readonly classification: string;
  readonly status: string;
  readonly updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class DocumentApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(AppSettings).value.apiUrl}/api/documents`;

  list(): Observable<readonly DocumentRecord[]> {
    return this.http.get<readonly DocumentRecord[]>(this.baseUrl);
  }

  create(title: string, classification: string): Observable<DocumentRecord> {
    return this.http.post<DocumentRecord>(this.baseUrl, { title, classification });
  }

  transition(id: number, action: 'submit' | 'approve' | 'reject'): Observable<DocumentRecord> {
    return this.http.post<DocumentRecord>(`${this.baseUrl}/${id}/${action}`, {});
  }
}
