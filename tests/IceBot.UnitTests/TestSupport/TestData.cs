using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.ArtifactTemplates.Queries;
using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Domain.RobotConfiguration.ArtifactTemplates;
using System.Reflection;
using Application.Identity.Tokens.Claims;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.RobotConfiguration.Artifacts;

namespace IceBot.UnitTests.TestSupport;

internal static class TestData
{
    public static CurrentUserContext SystemAdmin(Guid? accountId = null) => new()
    {
        AccountId = accountId ?? Guid.NewGuid(),
        IsSystemAdmin = true
    };

    public static RobotArtifact PublishedArtifact(
        Guid organizationId,
        string code = "PREPARE",
        string fileName = "prepare.lua",
        char checksumCharacter = 'a')
    {
        var artifact = RobotArtifact.CreateDraft(
            organizationId,
            code,
            code,
            $"robot-artifacts/{organizationId:D}/{fileName}",
            fileName,
            new string(checksumCharacter, 64),
            "FAIRINO_LUA_V1",
            "FR5",
            128,
            DateTimeOffset.UtcNow);
        artifact.AssignTechnicalContract(Guid.NewGuid(), new string('c', 64));
        artifact.Publish();
        return artifact;
    }

    public static RobotArtifactTemplate DraftTemplate(string code = "PREPARE")
    {
        var template = RobotArtifactTemplate.CreateDraft(
            code,
            code,
            $"robot-artifact-templates/{Guid.NewGuid():D}/template.lua",
            "template.lua",
            new string('b', 64),
            "FAIRINO_LUA_V1",
            "FR5",
            128,
            DateTimeOffset.UtcNow);
        template.AssignTechnicalContract(Guid.NewGuid(), new string('c', 64));
        return template;
    }

    public static ConfigurationRelease RetiredRelease(Guid organizationId)
    {
        var release = ConfigurationRelease.CreateDraft(organizationId, 1);
        SetProperty(release, nameof(ConfigurationRelease.Status), ConfigurationReleaseStatus.Retired);
        SetProperty(release, nameof(ConfigurationRelease.ReleaseChecksum), new string('c', 64));
        return release;
    }

    public static void SetProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(target, value);
    }
}
