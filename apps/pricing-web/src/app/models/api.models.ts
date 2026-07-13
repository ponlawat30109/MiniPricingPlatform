export type RuleType = 'TimeWindowPromotion' | 'RemoteAreaSurcharge' | 'WeightTier';
export interface PricingRule {
  id?: string;
  name: string;
  type: RuleType;
  priority: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isActive: boolean;
  discountPercentage?: number;
  surchargeAmount?: number;
  area?: string;
  minWeight?: number;
  maxWeight?: number | null;
  pricePerKg?: number;
  fromTime?: string | null;
  toTime?: string | null;
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
export interface BulkAcceptedResponse {
  job_id: string;
}
export type JobStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';
export interface JobResponse {
  jobId: string;
  status: JobStatus;
  results?: QuoteResponse[];
  failure?: { code: string; message: string };
}
export interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]> | { field: string; message: string }[];
}
