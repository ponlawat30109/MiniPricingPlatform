import { A11yModule } from '@angular/cdk/a11y';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  input,
  output,
  signal,
  inject,
  viewChild,
  ElementRef,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PricingRule, RuleType } from '../../models/api.models';
import { RuleApiClient } from '../../core/api/rule-api.client';
import { httpErrorMessage } from '../../core/http/problem-details';
import { DELIVERY_AREAS } from '../../utilities/delivery-areas';
import {
  RuleFormValue,
  createRuleFormValue,
  createRulePayload,
  validateRuleFormValue,
} from './rule-form.helpers';

@Component({
  selector: 'app-rule-editor',
  imports: [ReactiveFormsModule, A11yModule],
  templateUrl: './rule-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RuleEditorComponent {
  private readonly ruleApi = inject(RuleApiClient);
  readonly rule = input<PricingRule | null>(null);
  readonly saveRule = output<PricingRule>();
  // eslint-disable-next-line @angular-eslint/no-output-native
  readonly cancel = output<void>();
  readonly deliveryAreas = DELIVERY_AREAS;
  readonly saveError = signal('');
  readonly saving = signal(false);
  readonly initialFocus = viewChild<ElementRef<HTMLInputElement>>('initialFocus');
  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    type: new FormControl<RuleType>('WeightTier', { nonNullable: true }),
    priority: new FormControl(0, { nonNullable: true, validators: [Validators.min(0)] }),
    effectiveFrom: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    effectiveTo: new FormControl('', { nonNullable: true }),
    isActive: new FormControl(true, { nonNullable: true }),
    discountPercentage: new FormControl<number | null>(null),
    surchargeAmount: new FormControl<number | null>(null),
    area: new FormControl('', { nonNullable: true }),
    minWeight: new FormControl<number | null>(null),
    maxWeight: new FormControl<number | null>(null),
    pricePerKg: new FormControl<number | null>(null),
    fromTime: new FormControl('', { nonNullable: true }),
    toTime: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    queueMicrotask(() => {
      this.form.reset(createRuleFormValue(this.rule()));
      this.initialFocus()?.nativeElement.focus();
    });
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    this.cancel.emit();
  }

  submit(): void {
    const value = this.form.getRawValue() as RuleFormValue;
    const validationError = validateRuleFormValue(value);
    if (validationError) {
      this.saveError.set(validationError);
      this.form.markAllAsTouched();
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const payload = createRulePayload(value, this.rule()?.id);
    this.saving.set(true);
    const request = payload.id
      ? this.ruleApi.updateRule(payload)
      : this.ruleApi.createRule(payload);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.saveRule.emit(payload);
      },
      error: (error) => {
        this.saveError.set(httpErrorMessage(error));
        this.saving.set(false);
      },
    });
  }
}
