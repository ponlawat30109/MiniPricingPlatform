import { PricingRule } from '../models/api.models';

export const DELIVERY_AREAS = [
  'Bangkok',
  'Bangkok Metropolitan Region',
  'Other Provinces',
] as const;

export function reviewRuleCatalog(rules: PricingRule[], now = new Date()): string[] {
  const active = rules.filter((rule) => rule.isActive);
  const tiers = active
    .filter((rule) => rule.type === 'WeightTier')
    .sort((a, b) => (a.minWeight ?? 0) - (b.minWeight ?? 0));
  const issues: string[] = [];
  if (!tiers.length) issues.push('No active weight pricing is configured.');
  else {
    if ((tiers[0].minWeight ?? 0) > 0) issues.push('Weight coverage does not start at zero.');
    for (let index = 1; index < tiers.length; index++) {
      const previousMaximum = tiers[index - 1].maxWeight;
      const currentMinimum = tiers[index].minWeight ?? 0;
      if (previousMaximum == null)
        issues.push(`Weight tiers overlap after ${tiers[index - 1].name}.`);
      else if (currentMinimum > previousMaximum)
        issues.push(`Weight coverage has a gap before ${tiers[index].name}.`);
      else if (currentMinimum < previousMaximum)
        issues.push(`Weight tiers overlap around ${tiers[index].name}.`);
    }
    if (tiers.at(-1)?.maxWeight != null)
      issues.push('Weight coverage has no open-ended final tier.');
  }
  for (const rule of active) {
    if (rule.effectiveTo && new Date(rule.effectiveTo) < now)
      issues.push(`${rule.name} is active but expired.`);
    if (rule.type === 'TimeWindowPromotion' && !rule.effectiveTo)
      issues.push(`${rule.name} has no end date.`);
  }
  const areas = new Set<string>();
  for (const rule of active.filter((rule) => rule.type === 'RemoteAreaSurcharge')) {
    const area = rule.area?.trim().toLocaleLowerCase();
    if (area && areas.has(area)) issues.push(`Duplicate area surcharge target: ${rule.area}.`);
    if (area) areas.add(area);
  }
  return issues;
}
