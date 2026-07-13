import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BulkAcceptedResponse,
  JobResponse,
  QuoteRequest,
  QuoteResponse,
} from '../../models/api.models';

@Injectable({ providedIn: 'root' })
export class PricingApiClient {
  private readonly http = inject(HttpClient);

  quote(value: QuoteRequest): Observable<QuoteResponse> {
    return this.http.post<QuoteResponse>(`${environment.pricingServiceUrl}/quotes/price`, value);
  }

  submitJson(quotes: QuoteRequest[]): Observable<BulkAcceptedResponse> {
    return this.http.post<BulkAcceptedResponse>(`${environment.pricingServiceUrl}/quotes/bulk`, {
      quotes,
    });
  }

  submitCsv(file: File): Observable<BulkAcceptedResponse> {
    const body = new FormData();
    body.append('file', file);
    return this.http.post<BulkAcceptedResponse>(
      `${environment.pricingServiceUrl}/quotes/bulk`,
      body,
    );
  }

  job(id: string): Observable<JobResponse> {
    return this.http.get<JobResponse>(`${environment.pricingServiceUrl}/jobs/${id}`);
  }
}
