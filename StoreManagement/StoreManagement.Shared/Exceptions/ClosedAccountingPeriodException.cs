using System;

namespace StoreManagement.Shared.Exceptions;

/// <summary>
/// Exception thrown when an operation falls within a closed accounting period.
/// </summary>
public class ClosedAccountingPeriodException : InvalidOperationException
{
    public ClosedAccountingPeriodException(DateTime operationDate) 
        : base($"Transaction falls in a closed financial period (Operation Date: {operationDate:yyyy-MM-dd}). This accounting period is locked.")
    {
        OperationDate = operationDate;
    }

    public ClosedAccountingPeriodException(string message) : base(message)
    {
    }

    public DateTime? OperationDate { get; }
}
