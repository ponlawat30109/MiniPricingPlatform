import { describe, expect, it } from 'vitest';
import { csvToQuotes, downloadRows, formatMoney } from './console-utilities';
import { DELIVERY_AREAS, reviewRuleCatalog } from './delivery-areas';

describe('console utilities', () => {
  it('parses quote CSV rows', () => {
    expect(csvToQuotes('weight,area\n2.5,Bangkok')).toEqual([{ weight: 2.5, area: 'Bangkok' }]);
  });
  it('reports invalid CSV row numbers', () => {
    expect(() => csvToQuotes('weight,area\nnope,Bangkok')).toThrow('Row 2');
  });
  it('formats baht and exports results', () => {
    expect(formatMoney(150)).toContain('150.00');
    expect(
      downloadRows([
        { basePrice: 100, surcharges: 50, discounts: 0, totalPrice: 150, appliedRules: ['Base'] },
      ]),
    ).toContain('100,50,0,150,Base');
  });
  it('offers the agreed Thai operating areas in English', () => {
    expect(DELIVERY_AREAS).toEqual(['Bangkok', 'Bangkok Metropolitan Region', 'Other Provinces']);
  });
  it('reports catalog issues', () => {
    const issues = reviewRuleCatalog(
      [
        {
          name: 'A',
          type: 'WeightTier',
          priority: 1,
          effectiveFrom: '2026-01-01',
          isActive: true,
          minWeight: 0,
          maxWeight: 10,
          pricePerKg: 20,
        },
        {
          name: 'B',
          type: 'WeightTier',
          priority: 2,
          effectiveFrom: '2026-01-01',
          isActive: true,
          minWeight: 5,
          maxWeight: 20,
          pricePerKg: 25,
        },
        {
          name: 'Old',
          type: 'TimeWindowPromotion',
          priority: 3,
          effectiveFrom: '2025-01-01',
          effectiveTo: '2025-12-31',
          isActive: true,
          discountPercentage: 5,
        },
        {
          name: 'One',
          type: 'RemoteAreaSurcharge',
          priority: 4,
          effectiveFrom: '2026-01-01',
          isActive: true,
          area: 'Other Provinces',
          surchargeAmount: 50,
        },
        {
          name: 'Two',
          type: 'RemoteAreaSurcharge',
          priority: 5,
          effectiveFrom: '2026-01-01',
          isActive: true,
          area: 'other provinces',
          surchargeAmount: 30,
        },
      ],
      new Date('2026-07-13T00:00:00Z'),
    );
    expect(issues.join(' ')).toMatch(/overlap/i);
    expect(issues.join(' ')).toMatch(/coverage/i);
    expect(issues.join(' ')).toMatch(/expired/i);
    expect(issues.join(' ')).toMatch(/duplicate/i);
  });
});
