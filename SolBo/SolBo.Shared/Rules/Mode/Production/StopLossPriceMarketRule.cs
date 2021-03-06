﻿using NLog;
using SolBo.Shared.Domain.Configs;
using SolBo.Shared.Domain.Enums;
using SolBo.Shared.Domain.Statics;
using SolBo.Shared.Extensions;
using SolBo.Shared.Rules.Order;
using System.Collections.Generic;

namespace SolBo.Shared.Rules.Mode.Production
{
    public class StopLossPriceMarketRule : IMarketRule
    {
        private static readonly Logger Logger = LogManager.GetLogger("SOLBO");
        public MarketOrderType MarketOrder => MarketOrderType.STOPLOSS;
        private readonly ICollection<IOrderRule> _rules = new HashSet<IOrderRule>();
        public IRuleResult RuleExecuted(Solbot solbot)
        {
            _rules.Add(new AvailableAssetBaseRule());
            _rules.Add(new AvailableEnoughAssetBaseRule());
            _rules.Add(new StopLossPriceReachedRule());
            _rules.Add(new BoughtPriceBeforeSellAndStopLossRule());

            var result = true;

            foreach (var item in _rules)
            {
                var resultOrderStep = item.RuleExecuted(solbot);

                if (resultOrderStep.Success)
                    Logger.Info($"{MarketOrder.GetDescription()} => {resultOrderStep.Message}");
                else
                {
                    result = false;
                    Logger.Warn($"{MarketOrder.GetDescription()} => {resultOrderStep.Message}");
                }
            }

            solbot.Communication.StopLoss.IsReady = result;

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