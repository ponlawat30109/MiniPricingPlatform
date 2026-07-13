import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PricingApiClient } from '../../core/api/pricing-api.client';
import { httpErrorMessage } from '../../core/http/problem-details';
import { QuoteResponse } from '../../models/api.models';
import { formatMoney } from '../../utilities/console-utilities';
import { DELIVERY_AREAS } from '../../utilities/delivery-areas';

@Component({
  selector: 'app-quotes',
  imports: [ReactiveFormsModule],
  templateUrl: './quotes.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuotesComponent {
  private readonly pricingApi = inject(PricingApiClient);
  readonly deliveryAreas = DELIVERY_AREAS;
  readonly form = new FormGroup({
    weight: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01)]),
    area: new FormControl('', [Validators.required]),
  });
  readonly busy = signal(false);
  readonly error = signal('');
  readonly result = signal<QuoteResponse | null>(null);
  readonly money = formatMoney;

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const value = this.form.getRawValue();
    this.pricingApi.quote({ weight: value.weight!, area: value.area!.trim() }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.busy.set(false);
      },
      error: (error) => {
        this.error.set(httpErrorMessage(error));
        this.busy.set(false);
      },
    });
  }
}
