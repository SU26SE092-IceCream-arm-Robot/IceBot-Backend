using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Releases.Results;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Routes.Contracts;
using Application.ProductionConfiguration.Routes.Support;
using Application.RobotConfiguration.AuthoringImports.Composition;
using Application.Shared.Wrappers;
using Domain.Common;

namespace Application.RobotConfiguration.AuthoringImports.ReleaseLinkage;

public sealed record CreateRobotAuthoringReleaseDraftCommand(
    CurrentUserContext UserContext,
    Guid OrganizationId,
    Guid ImportId,
    Guid RecipeId,
    string? RequiredWorkcellCapabilityCode,
    IReadOnlyCollection<string> SupportedOptionCodes);

public sealed record RobotAuthoringReleaseDraftResult(
    RobotAuthoringImportResult Import,
    ConfigurationReleaseResult ConfigurationRelease);

public sealed class CreateRobotAuthoringReleaseDraftCommandHandler(
    IRobotAuthoringImportStore importStore,
    IRobotAuthoringCompositionStore compositionStore,
    IConfigurationReleaseStore releaseStore,
    CreateConfigurationReleaseCommandHandler createReleaseHandler,
    ReplaceConfigurationReleaseRoutesCommandHandler replaceRoutesHandler)
{
    public async Task<ApiResult<RobotAuthoringReleaseDraftResult>> HandleAsync(
        CreateRobotAuthoringReleaseDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.RecipeId == Guid.Empty)
            return Fail("Recipe is required.");

        var requestedCapabilityCode = NormalizeCode(command.RequiredWorkcellCapabilityCode);
        if (requestedCapabilityCode?.Length > 100)
            return Fail("Required workcell capability code must be at most 100 characters.");

        var optionCodes = command.SupportedOptionCodes
            .Select(NormalizeCode)
            .Where(code => code is not null)
            .Cast<string>()
            .ToArray();
        if (optionCodes.Length != command.SupportedOptionCodes.Count ||
            optionCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != optionCodes.Length)
        {
            return Fail("Supported option codes must be non-empty and unique.");
        }

        var transactionStarted = false;
        try
        {
            var importSession = await importStore.BeginMutationAsync(
                command.OrganizationId, command.ImportId, cancellationToken);
            transactionStarted = true;
            if (importSession is null)
                return await RollbackAndFailAsync("Robot authoring import not found.", 404, cancellationToken);

            if (!importSession.PublishedAt.HasValue || !importSession.AppliedRobotProgramId.HasValue)
                return await RollbackAndFailAsync(
                    "Import contracts, artifacts, and robot program must be published before release authoring.",
                    409,
                    cancellationToken);
            if (!importSession.ComposedRecipeId.HasValue || !importSession.CompositionConfirmedAt.HasValue)
                return await RollbackAndFailAsync(
                    "Confirm the Recipe composition before creating a configuration release draft.",
                    409,
                    cancellationToken);
            if (importSession.ComposedRecipeId.Value != command.RecipeId ||
                !importSession.GetComposedOptionCodes().Order(StringComparer.Ordinal)
                    .SequenceEqual(optionCodes.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            {
                return await RollbackAndFailAsync(
                    "Release recipe and supported option selection must match the confirmed robot composition.",
                    409,
                    cancellationToken);
            }

            var capabilityCode = requestedCapabilityCode;
            if (capabilityCode is null)
            {
                var contractIds = importSession.Items.Where(item => item.TechnicalContractId.HasValue)
                    .Select(item => item.TechnicalContractId!.Value).Distinct().ToArray();
                var contracts = await compositionStore.GetContractsAsync(command.OrganizationId, contractIds,
                    cancellationToken);
                var declaredCapabilities = contracts.SelectMany(contract => contract.Effects)
                    .Select(effect => NormalizeCode(effect.RequiredWorkcellCapabilityCode))
                    .Where(code => code is not null).Cast<string>().Distinct(StringComparer.Ordinal).ToArray();
                if (declaredCapabilities.Length != 1)
                    return await RollbackAndFailAsync(
                        "Required workcell capability must be selected when published artifact contracts do not declare exactly one capability.",
                        409,
                        cancellationToken);
                capabilityCode = declaredCapabilities[0];
            }

            var requiredCapabilitiesJson = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                requires = new[] { new { code = capabilityCode, required = true } }
            });

            if (importSession.LinkedConfigurationReleaseId.HasValue)
            {
                var existingRelease = await releaseStore.GetReleaseByIdAsync(
                    importSession.LinkedConfigurationReleaseId.Value, cancellationToken);
                if (existingRelease is null || existingRelease.OrganizationId != command.OrganizationId)
                    return await RollbackAndFailAsync("Linked configuration release was not found.", 409, cancellationToken);
                if (!MatchesRequestedRoute(existingRelease, command.RecipeId, capabilityCode,
                        requiredCapabilitiesJson, optionCodes))
                {
                    return await RollbackAndFailAsync(
                        "Robot authoring import is already linked using a different recipe, capability, or option selection.",
                        409,
                        cancellationToken);
                }

                await importStore.CommitMutationAsync(cancellationToken);
                transactionStarted = false;
                return ApiResult<RobotAuthoringReleaseDraftResult>.Success(
                    new RobotAuthoringReleaseDraftResult(
                        RobotAuthoringImportResult.From(importSession),
                        ConfigurationReleaseResult.FromEntity(existingRelease)),
                    "Existing robot authoring release draft returned.");
            }

            var createResult = await createReleaseHandler.HandleAsync(new CreateConfigurationReleaseCommand
            {
                UserContext = command.UserContext,
                OrganizationId = command.OrganizationId
            }, cancellationToken);
            if (!createResult.Succeeded || createResult.Data is null)
                return await RollbackAndFailAsync(
                    createResult.Message ?? "Configuration release draft could not be created.",
                    createResult.StatusCode,
                    cancellationToken);

            var routeCode = BuildRouteCode(importSession.ProposedProgramCode);
            var replaceResult = await replaceRoutesHandler.HandleAsync(new ReplaceConfigurationReleaseRoutesCommand
            {
                UserContext = command.UserContext,
                OrganizationId = command.OrganizationId,
                ReleaseId = createResult.Data.Id,
                ExpectedRevision = createResult.Data.Revision,
                Routes =
                [
                    new ConfigurationReleaseRouteInput(
                        command.RecipeId,
                        routeCode,
                        0,
                        ExecutionRouteRequiredCapabilitiesContract.ParseValidated(requiredCapabilitiesJson)
                            .Select(requirement => new ExecutionRouteCapabilityRequirementContract(
                                requirement.Code,
                                requirement.Required))
                            .ToArray(),
                        optionCodes,
                        [new ConfigurationReleaseRobotBindingInput(
                            importSession.AppliedRobotProgramId.Value,
                            1,
                            capabilityCode)])
                ]
            }, cancellationToken);
            if (!replaceResult.Succeeded || replaceResult.Data is null)
                return await RollbackAndFailAsync(
                    replaceResult.Message ?? "Configuration release route could not be created.",
                    replaceResult.StatusCode,
                    cancellationToken);

            importSession.LinkConfigurationRelease(createResult.Data.Id, DateTimeOffset.UtcNow,
                command.UserContext.AccountId);
            await importStore.CommitMutationAsync(cancellationToken);
            transactionStarted = false;

            return ApiResult<RobotAuthoringReleaseDraftResult>.Success(
                new RobotAuthoringReleaseDraftResult(
                    RobotAuthoringImportResult.From(importSession),
                    replaceResult.Data),
                "Draft configuration release created from the published robot authoring import.",
                201);
        }
        catch (DomainRuleException ex)
        {
            if (transactionStarted)
                await importStore.RollbackMutationAsync(CancellationToken.None);
            return Fail(ex.Message);
        }
        catch
        {
            if (transactionStarted)
                await importStore.RollbackMutationAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<ApiResult<RobotAuthoringReleaseDraftResult>> RollbackAndFailAsync(
        string message,
        int statusCode,
        CancellationToken cancellationToken)
    {
        await importStore.RollbackMutationAsync(CancellationToken.None);
        return Fail(message, statusCode);
    }

    private static ApiResult<RobotAuthoringReleaseDraftResult> Fail(string message, int statusCode = 400) =>
        ApiResult<RobotAuthoringReleaseDraftResult>.Fail(message, statusCode);

    private static string? NormalizeCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string BuildRouteCode(string programCode)
    {
        const string prefix = "AUTHORING_";
        var maximumProgramLength = 100 - prefix.Length;
        var value = programCode.Length <= maximumProgramLength
            ? programCode
            : programCode[..maximumProgramLength];
        return prefix + value;
    }

    private static bool MatchesRequestedRoute(
        Domain.ProductionConfiguration.Entities.ConfigurationRelease release,
        Guid recipeId,
        string capabilityCode,
        string requiredCapabilitiesJson,
        IReadOnlyCollection<string> optionCodes)
    {
        if (release.ExecutionRoutes.Count != 1) return false;
        var route = release.ExecutionRoutes.Single();
        var binding = route.RobotBindings.Count == 1 ? route.RobotBindings.Single() : null;
        return route.RecipeId == recipeId &&
               string.Equals(route.RequiredCapabilitiesJson, requiredCapabilitiesJson, StringComparison.Ordinal) &&
               route.GetSupportedOptionCodes().Order(StringComparer.Ordinal)
                   .SequenceEqual(optionCodes.Order(StringComparer.Ordinal), StringComparer.Ordinal) &&
               binding is not null &&
               string.Equals(binding.RequiredWorkcellCapabilityCode, capabilityCode, StringComparison.Ordinal);
    }
}
