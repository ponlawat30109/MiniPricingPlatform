import { PricingRule } from '../../models/api.models';
import {
  createDefaultRuleFormValue,
  createRuleFormValue,
  createRulePayload,
  validateRuleFormValue,
} from './rule-form.helpers';

describe('rule form helpers', () => {
  it('creates defaults for a new active weight-tier rule', () => {
    const value = createDefaultRuleFormValue('2026-07-13');

    expect(value.type).toBe('WeightTier');
    expect(value.effectiveFrom).toBe('2026-07-13');
    expect(value.isActive).toBe(true);
    expect(value.maxWeight).toBeNull();
  });

  it('maps an existing rule to date-only editor values', () => {
    const rule: PricingRule = {
      id: 'rule-1',
      name: 'Remote zone',
      type: 'RemoteAreaSurcharge',
      priority: 2,
      effectiveFrom: '2026-07-01T00:00:00.000Z',
      effectiveTo: '2026-07-31T00:00:00.000Z',
      isActive: false,
      area: 'Islands',
      surchargeAmount: 50,
    };

    const value = createRuleFormValue(rule, '2026-07-13');

    expect(value.effectiveFrom).toBe('2026-07-01');
    expect(value.effectiveTo).toBe('2026-07-31');
    expect(value.area).toBe('Islands');
    expect(value.isActive).toBe(false);
  });

  it('accepts an open-ended maximum weight', () => {
    const value = {
      ...createDefaultRuleFormValue('2026-07-13'),
      name: 'Standard',
      minWeight: 0,
      maxWeight: null,
      pricePerKg: 20,
    };

    expect(validateRuleFormValue(value)).toBeNull();
  });

  it('rejects invalid type-specific ranges', () => {
    const value = {
      ...createDefaultRuleFormValue('2026-07-13'),
      name: 'Invalid tier',
      minWeight: 5,
      maxWeight: 5,
      pricePerKg: 0,
    };

    expect(validateRuleFormValue(value)).toBe(
      'Complete the selected rule fields with valid ranges.',
    );
  });

  it('normalizes dates and preserves an existing id in the request payload', () => {
    const value = {
      ...createDefaultRuleFormValue('2026-07-13'),
      name: 'Standard',
      minWeight: 0,
      pricePerKg: 20,
    };

    const payload = createRulePayload(value, 'rule-1');

    expect(payload.id).toBe('rule-1');
    expect(payload.effectiveFrom).toBe('2026-07-13T00:00:00.000Z');
    expect(payload.effectiveTo).toBeNull();
  });

  it('builds a weight-tier payload without fields from other rule types', () => {
    const value = {
      ...createDefaultRuleFormValue('2026-07-13'),
      name: 'Heavy',
      minWeight: 5,
      pricePerKg: 20,
      area: 'stale area',
      surchargeAmount: 99,
      discountPercentage: 10,
    };

    const payload = createRulePayload(value);

    expect(payload.minWeight).toBe(5);
    expect(payload.area).toBeUndefined();
    expect(payload.surchargeAmount).toBeUndefined();
    expect(payload.discountPercentage).toBeUndefined();
  });

  it('normalizes blank optional promotion times to null', () => {
    const value = {
      ...createDefaultRuleFormValue('2026-07-13'),
      name: 'Lunch',
      type: 'TimeWindowPromotion' as const,
      discountPercentage: 10,
      fromTime: ' ',
      toTime: '',
    };

    const payload = createRulePayload(value);

    expect(payload.fromTime).toBeNull();
    expect(payload.toTime).toBeNull();
    expect(payload.minWeight).toBeUndefined();
  });
});
