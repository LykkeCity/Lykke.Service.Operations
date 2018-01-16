using System;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Service.Operations.Contracts
{
    public class CreateNewOrderCommand
    {
        /// <summary>
        /// Client's wallet Id inside Lykke
        /// </summary>
        [Required]
        public Guid WalletId { get; set; }

        /// <summary>
        /// Custom Id provided by the client for the created order
        /// </summary>
        [Required]
        public string ClientOrderId { get; set; }
    }
}
