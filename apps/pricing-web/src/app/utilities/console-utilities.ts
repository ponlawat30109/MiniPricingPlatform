import { QuoteRequest, QuoteResponse } from '../models/api.models';
export const formatMoney = (value: number) =>
  new Intl.NumberFormat('th-TH', { style: 'currency', currency: 'THB' }).format(value);
const splitCsv = (line: string) =>
  Array.from(line.matchAll(/(?:^|,)(?:"([^"]*(?:""[^"]*)*)"|([^,]*))/g), (m) =>
    (m[1] ?? m[2]).replaceAll('""', '"').trim(),
  );
export function csvToQuotes(text: string): QuoteRequest[] {
  const lines = text.trim().split(/\r?\n/);
  if (lines.length < 2) throw new Error('CSV needs a header and at least one row.');
  const headers = splitCsv(lines[0]).map((x) => x.toLowerCase());
  const wi = headers.indexOf('weight'),
    ai = headers.indexOf('area');
  if (wi < 0 || ai < 0) throw new Error('CSV headers must include weight and area.');
  return lines.slice(1).map((line, i) => {
    const cells = splitCsv(line),
      weight = Number(cells[wi]),
      area = cells[ai]?.trim();
    if (!Number.isFinite(weight) || weight <= 0 || !area)
      throw new Error(`Row ${i + 2} needs a positive weight and area.`);
    return { weight, area };
  });
}
const quote = (value: unknown) => {
  const text = String(value ?? '');
  return /[",\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
};
export function downloadRows(rows: QuoteResponse[]): string {
  return [
    'basePrice,surcharges,discounts,totalPrice,appliedRules',
    ...rows.map((r) =>
      [r.basePrice, r.surcharges, r.discounts, r.totalPrice, r.appliedRules.join('; ')]
        .map(quote)
        .join(','),
    ),
  ].join('\n');
}
