using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ProtoBuf;

namespace Lykke.Service.Operations.Contracts.Commands
{
    [ProtoContract]
    public class CreateNewOrderCommand : IValidatableObject
    {
        /// <summary>
        /// Client's wallet Id inside Lykke
        /// </summary>
        [ProtoMember(1)]
        public Guid WalletId { get; set; }

        /// <summary>
        /// Custom Id provided by the client for the created order
        /// </summary>
        [ProtoMember(2)]
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string ClientOrderId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (WalletId == Guid.Empty)
            {
                return new[] { new ValidationResult("WalletId must be not empty and has a correct GUID value", new[] { nameof(WalletId) }) };
            }

            return Array.Empty<ValidationResult>();
        }
    }
}
