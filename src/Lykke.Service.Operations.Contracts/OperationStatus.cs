using System;

namespace Lykke.Service.Operations.Contracts
{
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
