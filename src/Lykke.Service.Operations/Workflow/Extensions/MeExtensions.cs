﻿using Lykke.MatchingEngine.Connector.Models.Api;

namespace Lykke.Service.Operations.Workflow.Extensions
{
    public static class MeExtensions
    {
        public static string Format(this MeStatusCodes status)
        {
            switch (status)
            {
                case MeStatusCodes.NoLiquidity:
                    return "There is not enough liquidity in the order book. Please try to send smaller order.";
                case MeStatusCodes.LowBalance:
                case MeStatusCodes.NotEnoughFunds:
                    return "Not enough funds.";
                case MeStatusCodes.LeadToNegativeSpread:
                    return "This order has a negative spread with you orders.";
                case MeStatusCodes.InvalidPrice:
                    return "Price must be greather than zero";
                default:
                    return "We are experiencing technical problems. Please try again.";
            }
        }

        public static string GetStringCode(this MeStatusCodes status)
        {
            switch (status)
            {
                case MeStatusCodes.LowBalance:
                case MeStatusCodes.NotEnoughFunds:
                    return "NotEnoughFunds";
                default:
                    return "InternalError";
            }
        }
    }
}
