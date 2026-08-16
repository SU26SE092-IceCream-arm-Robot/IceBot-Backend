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
    public async Task Deploy_DoesNotBlockWhenInventoryTopologyIsNotConfigured()
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
                HasConfiguredInventoryTopology = false,
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
    public async Task Deploy_BlocksWhenConfiguredTopologyReportsNotReady()
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
                HasConfiguredInventoryTopology = true,
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
