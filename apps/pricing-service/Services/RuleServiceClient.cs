using PricingService.Models;

namespace PricingService.Services;

public interface IRuleServiceClient
{
    Task<IReadOnlyList<PricingRuleDto>> GetRulesAsync(CancellationToken cancellationToken);
}

public sealed class RuleServiceClient(HttpClient httpClient) : IRuleServiceClient
{
    public async Task<IReadOnlyList<PricingRuleDto>> GetRulesAsync(CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<List<PricingRuleDto>>("/rules", cancellationToken) ?? [];
}
