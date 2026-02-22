export const RULE_SERVICE_URL =
  process.env.NEXT_PUBLIC_RULE_SERVICE_URL || "http://localhost:5000";
export const PRICING_SERVICE_URL =
  process.env.NEXT_PUBLIC_PRICING_SERVICE_URL || "http://localhost:8080";

export interface PricingRule {
  id?: string;
  name: string;
  type: "TimeWindowPromotion" | "RemoteAreaSurcharge" | "WeightTier";
  priority: number;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
  // Specific fields
  discountPercentage?: number;
  surchargeAmount?: number;
  area?: string;
  minWeight?: number;
  maxWeight?: number;
  pricePerKg?: number;
  fromTime?: string;
  toTime?: string;
}

export interface QuoteRequest {
  weight: number;
  area: string;
}

export interface QuoteResponse {
  basePrice: number;
  surcharges: number;
  discounts: number;
  totalPrice: number;
  appliedRules: string[];
}

export async function fetchRules(): Promise<PricingRule[]> {
  const res = await fetch(`${RULE_SERVICE_URL}/rules`);
  if (!res.ok) throw new Error("Failed to fetch rules");
  return res.json();
}

export async function addRule(rule: PricingRule): Promise<void> {
  let endpoint = "/rules";
  if (rule.type === "TimeWindowPromotion") endpoint += "/promotion";
  else if (rule.type === "RemoteAreaSurcharge") endpoint += "/surcharge";
  else if (rule.type === "WeightTier") endpoint += "/weight-tier";

  const res = await fetch(`${RULE_SERVICE_URL}${endpoint}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(rule),
  });
  if (!res.ok) throw new Error("Failed to add rule");
}

export async function calculatePrice(
  request: QuoteRequest,
): Promise<QuoteResponse> {
  const res = await fetch(`${PRICING_SERVICE_URL}/quotes/price`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error("Failed to calculate price");
  return res.json();
}
