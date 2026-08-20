using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Application.ProductionConfiguration.Readiness;
using Application.ProductionConfiguration.Readiness.Services;
using Domain.Inventory.Enums;
using Domain.ProductionConfiguration.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class ProductionInventoryReadinessGuardTests
{
    [Fact]
    public async Task Deploy_DoesNotBlockWhenInventoryBalanceIsNotConfigured()
    {
        var evaluator = Substitute.For<IInventoryReadinessEvaluator>();
        evaluator.EvaluateKioskAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<InventoryReadinessRouteInput>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<InventoryReadinessEvaluationOptions?>())
            .Returns(new KioskInventoryReadinessResult
            {
                KioskId = Guid.NewGuid(),
                HasConfiguredInventoryBalance = false,
                IsReady = false,
                OverallStatus = InventoryReadinessStatus.MissingIngredient
            });
        var guard = new ProductionInventoryReadinessGuard(
            evaluator,
            Options.Create(new InventoryReadinessPolicyOptions
            {
                DeployPolicy = InventoryReadinessPolicy.Block
            }));

        var assessment = await guard.EvaluateDeployAsync(
            ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1),
            Guid.NewGuid());

        Assert.False(assessment.HasWarnings);
        Assert.False(assessment.IsBlocked);
    }

    [Fact]
    public async Task Deploy_BlocksWhenConfiguredInventoryBalanceReportsNotReady()
    {
        var evaluator = Substitute.For<IInventoryReadinessEvaluator>();
        evaluator.EvaluateKioskAsync(
                Arg.Any<Guid>(),
                Arg.Any<IReadOnlyCollection<InventoryReadinessRouteInput>>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<InventoryReadinessEvaluationOptions?>())
            .Returns(new KioskInventoryReadinessResult
            {
                KioskId = Guid.NewGuid(),
                HasConfiguredInventoryBalance = true,
                IsReady = false,
                OverallStatus = InventoryReadinessStatus.MissingIngredient
            });
        var guard = new ProductionInventoryReadinessGuard(
            evaluator,
            Options.Create(new InventoryReadinessPolicyOptions
            {
                DeployPolicy = InventoryReadinessPolicy.Block
            }));

        var assessment = await guard.EvaluateDeployAsync(
            ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1),
            Guid.NewGuid());

        Assert.True(assessment.HasWarnings);
        Assert.True(assessment.IsBlocked);
    }
}
