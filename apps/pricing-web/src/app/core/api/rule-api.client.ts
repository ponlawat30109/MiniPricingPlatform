import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PricingRule } from '../../models/api.models';

@Injectable({ providedIn: 'root' })
export class RuleApiClient {
  private readonly http = inject(HttpClient);

  listRules(): Observable<PricingRule[]> {
    return this.http.get<PricingRule[]>(`${environment.ruleServiceUrl}/rules`);
  }

  createRule(rule: PricingRule): Observable<PricingRule> {
    const path =
      rule.type === 'TimeWindowPromotion'
        ? 'promotion'
        : rule.type === 'RemoteAreaSurcharge'
          ? 'surcharge'
          : 'weight-tier';
    return this.http.post<PricingRule>(`${environment.ruleServiceUrl}/rules/${path}`, rule);
  }

  updateRule(rule: PricingRule): Observable<PricingRule> {
    return this.http.put<PricingRule>(`${environment.ruleServiceUrl}/rules/${rule.id}`, rule);
  }

  deleteRule(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.ruleServiceUrl}/rules/${id}`);
  }
}
