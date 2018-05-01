using System;

namespace Lykke.Service.Operations.Contracts
{
    /// <summary>
    /// Possible operation statuses
    /// </summary>
    [Flags]
    public enum OperationStatus
    {
        Created,
        Accepted,
        Confirmed,
        Completed,
        Canceled,
        Failed
    }
}
