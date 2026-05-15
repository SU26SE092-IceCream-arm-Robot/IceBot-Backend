namespace Domain.Enums;

public enum EntityStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Disabled = 4,
    Archived = 5
}

public enum AccountStatus
{
    Active = 1,
    PendingVerification = 2,
    Suspended = 3,
    Disabled = 4
}

public enum KioskStatus
{
    Provisioning = 1,
    Active = 2,
    Offline = 3,
    Maintenance = 4,
    Disabled = 5,
    Retired = 6
}

public enum DeviceStatus
{
    Provisioning = 1,
    Online = 2,
    Offline = 3,
    Maintenance = 4,
    Error = 5,
    Disabled = 6,
    Retired = 7
}

public enum OrderStatus
{
    Draft = 1,
    PendingPayment = 2,
    Paid = 3,
    Accepted = 4,
    Preparing = 5,
    Ready = 6,
    Completed = 7,
    Cancelled = 8,
    Failed = 9
}

public enum OrderChannel
{
    Tablet = 1,
    Kiosk = 2,
    MobileApp = 3,
    Web = 4,
    Admin = 5,
    External = 6
}

public enum PaymentStatus
{
    Unpaid = 1,
    Authorized = 2,
    Paid = 3,
    PartiallyRefunded = 4,
    Refunded = 5,
    Failed = 6,
    Cancelled = 7
}

public enum OrderItemStatus
{
    Pending = 1,
    Accepted = 2,
    Preparing = 3,
    Completed = 4,
    Cancelled = 5,
    Failed = 6
}

public enum PaymentTransactionStatus
{
    Pending = 1,
    Authorized = 2,
    Paid = 3,
    Failed = 4,
    Cancelled = 5,
    Refunded = 6,
    Expired = 7
}

public enum PaymentCallbackProcessingStatus
{
    Received = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
    Ignored = 5
}

public enum RefundStatus
{
    Requested = 1,
    Processing = 2,
    Processed = 3,
    Rejected = 4,
    Failed = 5,
    Cancelled = 6
}

public enum RobotProgramScopeType
{
    Global = 1,
    Organization = 2,
    Store = 3,
    Kiosk = 4,
    Device = 5
}

public enum RobotProgramStatus
{
    Draft = 1,
    Published = 2,
    Active = 3,
    Retired = 4,
    Disabled = 5
}

public enum CalibrationStatus
{
    NotRequired = 1,
    Pending = 2,
    InProgress = 3,
    Calibrated = 4,
    Validated = 5,
    Failed = 6
}

public enum RobotJobStatus
{
    Queued = 1,
    Running = 2,
    Paused = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}

public enum RobotJobStepStatus
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Skipped = 5,
    Cancelled = 6
}

public enum RecipeStatus
{
    Draft = 1,
    Published = 2,
    Active = 3,
    Retired = 4
}

public enum SeverityLevel
{
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

public enum AlertStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3,
    Suppressed = 4
}

public enum MaintenanceTicketStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    Resolved = 4,
    Closed = 5,
    Cancelled = 6
}

public enum MaintenancePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum OptionSelectionType
{
    Single = 1,
    Multiple = 2
}

public enum SyncEventStatus
{
    Received = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4,
    DeadLettered = 5,
    Ignored = 6
}

public enum SyncDeadLetterStatus
{
    Open = 1,
    Resolved = 2,
    Ignored = 3
}

public enum KioskHeartbeatStatus
{
    Online = 1,
    Degraded = 2,
    Offline = 3
}

public enum IngredientLevelStatus
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    Full = 3
}
