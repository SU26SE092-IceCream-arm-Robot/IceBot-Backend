using System.Text.Json.Serialization;

namespace Domain.RobotConfiguration.Programs;

[JsonConverter(typeof(JsonStringEnumConverter<RobotProgramRestartPolicy>))]
public enum RobotProgramRestartPolicy
{
    ManualOnly = 0,
    NotRestartable = 1,
    RestartFromBeginningIfNoPhysicalOutput = 2,
    ResumeFromCheckpoint = 3
}
