using Domain.Devices.ExecutionEndpoints;
namespace Domain.Devices.ExecutionEndpoints;

public enum ExecutionSafetyState { Unknown = 0, Safe = 1, Interlocked = 2, EmergencyStopped = 3 }
