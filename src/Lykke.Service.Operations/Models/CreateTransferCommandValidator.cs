﻿using FluentValidation;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;

namespace Lykke.Service.Operations.Models
{
    public class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
    {
        public CreateTransferCommandValidator()
        {
            RuleFor(m => m.ClientId)
                .NotNull();                

            RuleFor(m => m.AssetId)
                .NotEmpty();

            RuleFor(m => m.Amount)
                .GreaterThan(0);

            RuleFor(m => m.SourceWalletId)
                .NotEmpty();

            RuleFor(m => m.WalletId)
                .NotEmpty();
        }
    }
}
