namespace Domain.Orders.Incidents;

public enum ProductionIncidentStatus
{
    Open = 0,
    AwaitingInspection = 1,
    ResolutionSelected = 2,
    ResolutionInProgress = 3,
    Resolved = 4,
    Cancelled = 5
}

public enum ProductionIncidentTrigger
{
    ExecutionFailed = 0,
    ManualInterventionRequired = 1,
    DefectiveOutputReported = 2,
    OutcomeUnknown = 3
}

public enum ProductionInspectionOutcome
{
    ConfirmedGood = 0,
    NotProduced = 1,
    Defective = 2,
    PartialOrUncertain = 3,
    Unknown = 4
}

public enum ProductionIncidentResolution
{
    DeliverExistingOutput = 0,
    DiscardProduct = 1,
    RequestRemake = 2,
    RequestRefund = 3,
    IssueVoucher = 4,
    AwaitTechnicalReview = 5,
    NoAction = 6
}

