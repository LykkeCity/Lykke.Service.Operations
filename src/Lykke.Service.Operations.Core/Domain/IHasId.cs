using System;

namespace Lykke.Service.Operations.Core.Domain
{
    public interface IHasId
    {
        Guid Id { get; set; }
    }
}
