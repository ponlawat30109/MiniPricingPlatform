"use client";

import { useState, useEffect } from "react";
import {
  fetchRules,
  calculatePrice,
  addRule,
  PricingRule,
  QuoteResponse,
} from "@/lib/api";

export default function Home() {
  const [rules, setRules] = useState<PricingRule[]>([]);
  const [quoteRequest, setQuoteRequest] = useState({ weight: 0, area: "" });
  const [quoteResult, setQuoteResult] = useState<QuoteResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadRules();
  }, []);

  const loadRules = async () => {
    try {
      setLoading(true);
      const data = await fetchRules();
      const filteredRules = data.filter(
        (r) => r.name.toLowerCase() !== "string",
      );
      setRules(filteredRules);
    } catch (err) {
      setError("Failed to load rules. Make sure RuleService is running.");
    } finally {
      setLoading(false);
    }
  };

  const handleCalculate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      const result = await calculatePrice(quoteRequest);
      setQuoteResult(result);
      setError(null);
    } catch (err) {
      setError(
        "Failed to calculate price. Make sure PricingService is running.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="min-h-screen bg-slate-950 text-slate-100 p-8 font-sans">
      <div className="max-w-6xl mx-auto space-y-12">
        <Header />

        {error && (
          <div className="bg-red-500/10 border border-red-500/50 text-red-500 p-4 rounded-xl text-center">
            {error}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
          <PriceCalculator
            quoteRequest={quoteRequest}
            setQuoteRequest={setQuoteRequest}
            handleCalculate={handleCalculate}
            loading={loading}
            rules={rules}
            quoteResult={quoteResult}
          />

          <ActiveRulesList rules={rules} loadRules={loadRules} />
        </div>
      </div>
    </main>
  );
}

// Sub-components-Header
function Header() {
  return (
    <header className="text-center space-y-4">
      <h1 className="text-5xl font-extrabold tracking-tight bg-gradient-to-r from-blue-400 to-indigo-500 bg-clip-text text-transparent">
        Pricing Platform Dashboard
      </h1>
      <p className="text-slate-400 text-lg">
        Manage dynamic pricing rules and calculate real-time quotes.
      </p>
    </header>
  );
}

// Sub-components-PriceCalculator
function PriceCalculator({
  quoteRequest,
  setQuoteRequest,
  handleCalculate,
  loading,
  rules,
  quoteResult,
}: {
  quoteRequest: { weight: number; area: string };
  setQuoteRequest: (req: { weight: number; area: string }) => void;
  handleCalculate: (e: React.FormEvent) => Promise<void>;
  loading: boolean;
  rules: PricingRule[];
  quoteResult: QuoteResponse | null;
}) {
  return (
    <section className="space-y-6">
      <h2 className="text-2xl font-bold border-b border-slate-800 pb-2">
        Price Calculator
      </h2>
      <form
        onSubmit={handleCalculate}
        className="bg-slate-900/50 backdrop-blur-xl border border-white/5 p-8 rounded-3xl shadow-2xl space-y-4"
      >
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-400">
            Weight (kg)
          </label>
          <input
            type="number"
            value={quoteRequest.weight}
            onChange={(e) =>
              setQuoteRequest({
                ...quoteRequest,
                weight: Number(e.target.value),
              })
            }
            className="w-full bg-slate-800 border-white/10 rounded-xl p-3 focus:ring-2 focus:ring-blue-500 outline-none transition-all"
            placeholder="e.g. 15.5"
          />
          <p className="text-[10px] text-slate-500 mt-1">
            Supported weight ranges:{" "}
            {rules
              .filter((r) => r.type === "WeightTier" && r.isActive)
              .map((r) => `${r.minWeight}-${r.maxWeight || "∞"} kg`)
              .join(", ") || "None"}
          </p>
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-400">Area</label>
          <input
            type="text"
            value={quoteRequest.area}
            onChange={(e) =>
              setQuoteRequest({ ...quoteRequest, area: e.target.value })
            }
            className="w-full bg-slate-800 border-white/10 rounded-xl p-3 focus:ring-2 focus:ring-blue-500 outline-none transition-all"
            placeholder="e.g. Mountain"
          />
        </div>
        <button
          type="submit"
          disabled={loading}
          className="w-full bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 p-3 rounded-xl font-bold transition-all disabled:opacity-50"
        >
          {loading ? "Calculating..." : "Calculate Quote"}
        </button>
      </form>

      {/* QuoteResult */}
      {quoteResult && (
        <div className="bg-slate-900/80 border border-blue-500/30 p-8 rounded-3xl shadow-2xl space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
          <div className="grid grid-cols-2 gap-4">
            <div className="p-4 bg-slate-800/50 rounded-2xl">
              <p className="text-sm text-slate-400">Base Price</p>
              <p className="text-2xl font-bold">
                ฿{quoteResult.basePrice.toFixed(2)}
              </p>
            </div>
            <div className="p-4 bg-slate-800/50 rounded-2xl">
              <p className="text-sm text-slate-400">Total Price</p>
              <p className="text-2xl font-bold text-green-400">
                ฿{quoteResult.totalPrice.toFixed(2)}
              </p>
            </div>
          </div>
          {quoteResult.appliedRules.length > 0 && (
            <div className="space-y-2">
              <p className="text-sm font-medium text-slate-400">
                Applied Rules
              </p>
              <div className="flex flex-wrap gap-2">
                {quoteResult.appliedRules.map((rule, idx) => (
                  <span
                    key={idx}
                    className="bg-blue-900/30 text-blue-400 px-3 py-1 rounded-full text-xs font-medium border border-blue-500/20"
                  >
                    {rule}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </section>
  );
}

// Sub-components-ActiveRulesList
function ActiveRulesList({
  rules,
  loadRules,
}: {
  rules: PricingRule[];
  loadRules: () => void;
}) {
  return (
    <section className="space-y-6">
      <div className="flex justify-between items-center border-b border-slate-800 pb-2">
        <h2 className="text-2xl font-bold">Active Rules</h2>
        <button
          onClick={loadRules}
          className="text-sm text-blue-400 hover:text-blue-300"
        >
          Refresh
        </button>
      </div>
      <div className="space-y-4 h-[600px] overflow-y-auto pr-2 custom-scrollbar">
        {rules.map((rule) => (
          <div
            key={rule.id}
            className="bg-slate-900/40 border border-white/5 p-6 rounded-2xl hover:border-white/10 transition-colors"
          >
            <div className="flex justify-between items-start mb-4">
              <div>
                <h3 className="font-bold text-lg">{rule.name}</h3>
              </div>
              <span
                className={`px-2 py-1 rounded text-[10px] font-bold uppercase ${rule.isActive ? "bg-green-500/10 text-green-500" : "bg-red-500/10 text-red-500"}`}
              >
                {rule.isActive ? "Active" : "Inactive"}
              </span>
            </div>
            <div className="grid grid-cols-2 gap-y-2 text-sm text-slate-400">
              {rule.discountPercentage && (
                <div>
                  Discount:{" "}
                  <span className="text-green-400">
                    {rule.discountPercentage}%
                  </span>
                </div>
              )}
              {rule.surchargeAmount && (
                <div>
                  Surcharge:{" "}
                  <span className="text-red-400">฿{rule.surchargeAmount}</span>
                </div>
              )}
              {rule.area && (
                <div>
                  Area: <span className="text-white">{rule.area}</span>
                </div>
              )}
              {rule.pricePerKg && (
                <div>
                  Price/kg:{" "}
                  <span className="text-white">฿{rule.pricePerKg}</span>
                </div>
              )}
              {rule.type === "WeightTier" && (
                <div className="col-span-2 mt-1 pt-2 border-t border-white/5">
                  Weight Range:{" "}
                  <span className="text-blue-400 font-bold">
                    {rule.minWeight} - {rule.maxWeight || "∞"} kg
                  </span>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
