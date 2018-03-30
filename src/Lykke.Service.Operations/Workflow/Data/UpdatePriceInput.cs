using System;

namespace Lykke.Service.Operations.Workflow.Data
{
    public class UpdatePriceInput
    {
        public Guid Id { get; set; }
        public decimal Price { get; set; }
    }
}
