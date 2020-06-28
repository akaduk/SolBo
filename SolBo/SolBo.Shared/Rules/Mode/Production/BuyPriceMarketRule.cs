﻿using SolBo.Shared.Domain.Configs;
using SolBo.Shared.Domain.Enums;
using SolBo.Shared.Domain.Statics;

namespace SolBo.Shared.Rules.Mode.Production
{
    public class BuyPriceMarketRule : IMarketRule
    {
        public MarketOrderType MarketOrder => MarketOrderType.BUYING;
        public IRuleResult RuleExecuted(Solbot solbot)
        {
            var availableFund = solbot.Communication.AvailableAsset.Quote * solbot.Strategy.AvailableStrategy.FundPercentage;

            var result = solbot.Communication.Buy.AvailableFund > 0.0m &&
                solbot.Communication.Buy.AvailableFund > solbot.Communication.Symbol.MinNotional &&
                solbot.Communication.Buy.PriceReached;

            solbot.Communication.Buy.IsReady = result;

            return new MarketRuleResult()
            {
                Success = result,
                Message = result
                    ? LogGenerator.PriceMarketSuccess(MarketOrder)
                    : LogGenerator.PriceMarketError(MarketOrder)
            };
        }
    }
}