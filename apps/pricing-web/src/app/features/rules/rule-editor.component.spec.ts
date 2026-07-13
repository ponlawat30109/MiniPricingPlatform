import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { RuleApiClient } from '../../core/api/rule-api.client';
import { PricingRule } from '../../models/api.models';
import { RuleEditorComponent } from './rule-editor.component';

describe('RuleEditorComponent', () => {
  let api: { createRule: ReturnType<typeof vi.fn>; updateRule: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = { createRule: vi.fn(), updateRule: vi.fn() };
    TestBed.configureTestingModule({
      imports: [RuleEditorComponent],
      providers: [{ provide: RuleApiClient, useValue: api }],
    }).overrideComponent(RuleEditorComponent, { set: { template: '' } });
  });

  it('maps an edit rule into the form', async () => {
    const fixture = TestBed.createComponent(RuleEditorComponent);
    const rule: PricingRule = {
      id: 'rule-1',
      name: 'Remote',
      type: 'RemoteAreaSurcharge',
      priority: 1,
      effectiveFrom: '2026-07-01T00:00:00.000Z',
      effectiveTo: null,
      isActive: true,
      area: 'Islands',
      surchargeAmount: 50,
    };
    fixture.componentRef.setInput('rule', rule);
    await fixture.whenStable();

    expect(fixture.componentInstance.form.controls.name.value).toBe('Remote');
    expect(fixture.componentInstance.form.controls.effectiveFrom.value).toBe('2026-07-01');
  });

  it('saves a valid create rule and emits its payload', () => {
    api.createRule.mockReturnValue(of({}));
    const component = TestBed.createComponent(RuleEditorComponent).componentInstance;
    const saved: PricingRule[] = [];
    component.saveRule.subscribe((rule) => saved.push(rule));
    component.form.patchValue({
      name: 'Standard',
      effectiveFrom: '2026-07-13',
      minWeight: 0,
      pricePerKg: 20,
    });

    component.submit();

    expect(api.createRule).toHaveBeenCalledTimes(1);
    expect(saved[0].name).toBe('Standard');
  });

  it('emits cancel on escape', () => {
    const component = TestBed.createComponent(RuleEditorComponent).componentInstance;
    let cancelled = false;
    component.cancel.subscribe(() => (cancelled = true));
    component.closeOnEscape();
    expect(cancelled).toBe(true);
  });
});
