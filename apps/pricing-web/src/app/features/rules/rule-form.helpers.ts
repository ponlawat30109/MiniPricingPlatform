import { PricingRule, RuleType } from '../../models/api.models';

export interface RuleFormValue {
  name: string;
  type: RuleType;
  priority: number;
  effectiveFrom: string;
  effectiveTo: string;
  isActive: boolean;
  discountPercentage: number | null;
  surchargeAmount: number | null;
  area: string;
  minWeight: number | null;
  maxWeight: number | null;
  pricePerKg: number | null;
  fromTime: string;
  toTime: string;
}

export function createDefaultRuleFormValue(
  today = new Date().toISOString().slice(0, 10),
): RuleFormValue {
  return {
    name: '',
    type: 'WeightTier',
    priority: 0,
    effectiveFrom: today,
    effectiveTo: '',
    isActive: true,
    discountPercentage: null,
    surchargeAmount: null,
    area: '',
    minWeight: null,
    maxWeight: null,
    pricePerKg: null,
    fromTime: '',
    toTime: '',
  };
}

export function createRuleFormValue(rule: PricingRule | null, today?: string): RuleFormValue {
  const defaults = createDefaultRuleFormValue(today);
  if (!rule) return defaults;

  return {
    ...defaults,
    ...rule,
    effectiveFrom: rule.effectiveFrom.slice(0, 10),
    effectiveTo: rule.effectiveTo?.slice(0, 10) ?? '',
    discountPercentage: rule.discountPercentage ?? null,
    surchargeAmount: rule.surchargeAmount ?? null,
    minWeight: rule.minWeight ?? null,
    maxWeight: rule.maxWeight ?? null,
    pricePerKg: rule.pricePerKg ?? null,
    fromTime: rule.fromTime ?? '',
    toTime: rule.toTime ?? '',
  };
}

export function validateRuleFormValue(value: RuleFormValue): string | null {
  if (value.effectiveTo && value.effectiveTo < value.effectiveFrom) {
    return 'Effective to must be on or after effective from.';
  }

  const typeInvalid =
    value.type === 'WeightTier'
      ? value.minWeight === null ||
        value.pricePerKg === null ||
        value.minWeight < 0 ||
        (value.maxWeight !== null && value.maxWeight <= value.minWeight) ||
        value.pricePerKg <= 0
      : value.type === 'RemoteAreaSurcharge'
        ? !value.area.trim() || value.surchargeAmount === null || value.surchargeAmount < 0
        : value.discountPercentage === null ||
          value.discountPercentage <= 0 ||
          value.discountPercentage > 100;

  return typeInvalid ? 'Complete the selected rule fields with valid ranges.' : null;
}

export function createRulePayload(value: RuleFormValue, id?: string): PricingRule {
  const common = {
    id,
    name: value.name,
    type: value.type,
    priority: value.priority,
    effectiveFrom: new Date(value.effectiveFrom).toISOString(),
    effectiveTo: value.effectiveTo ? new Date(value.effectiveTo).toISOString() : null,
    isActive: value.isActive,
  };

  switch (value.type) {
    case 'WeightTier':
      return {
        ...common,
        minWeight: value.minWeight!,
        maxWeight: value.maxWeight,
        pricePerKg: value.pricePerKg!,
      };
    case 'RemoteAreaSurcharge':
      return {
        ...common,
        area: value.area.trim(),
        surchargeAmount: value.surchargeAmount!,
      };
    case 'TimeWindowPromotion':
      return {
        ...common,
        discountPercentage: value.discountPercentage!,
        fromTime: value.fromTime.trim() || null,
        toTime: value.toTime.trim() || null,
      };
  }
}
