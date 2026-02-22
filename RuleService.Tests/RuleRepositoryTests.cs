using RuleService.Models;
using RuleService.Repositories;
using Xunit;

namespace RuleService.Tests;

public class RuleRepositoryTests
{
    [Fact]
    public async Task AddAsync_AddsRuleToList()
    {
        // Arrange
        var repo = new JsonRuleRepository();
        var rule = new TimeWindowPromotionRule { Name = "Test Rule Unique 123", DiscountPercentage = 5 };

        // Act
        var (success, _) = await repo.AddAsync(rule);
        var rules = await repo.GetAllAsync();

        // Assert
        Assert.True(success);
        Assert.Contains(rules, r => r.Name == "Test Rule Unique 123");
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ReturnsFalse()
    {
        // Arrange
        var repo = new JsonRuleRepository();
        var rule1 = new TimeWindowPromotionRule { Name = "Duplicate Rule", DiscountPercentage = 5 };
        var rule2 = new TimeWindowPromotionRule { Name = "duplicate rule", DiscountPercentage = 10 }; // Same name, different case

        // Act
        await repo.AddAsync(rule1);
        var (success, error) = await repo.AddAsync(rule2);

        // Assert
        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRuleFromList()
    {
        // Arrange
        var repo = new JsonRuleRepository();
        var rules = await repo.GetAllAsync();
        var firstRule = rules.First();

        // Act
        await repo.DeleteAsync(firstRule.Id);
        var updatedRules = await repo.GetAllAsync();

        // Assert
        Assert.DoesNotContain(updatedRules, r => r.Id == firstRule.Id);
    }
}
