﻿using Kucoin.Net.Interfaces;
using NLog;
using SolBo.Shared.Domain.Configs;
using SolBo.Shared.Domain.Enums;
using SolBo.Shared.Domain.Statics;
using SolBo.Shared.Services;

namespace SolBo.Shared.Rules.Mode.Production
{
    public class KucoinStopLossExecuteMarketRule : IMarketRule
    {
        private static readonly Logger Logger = LogManager.GetLogger("SOLBO");
        public MarketOrderType MarketOrder => MarketOrderType.STOPLOSS;
        private readonly IKucoinClient _kucoinClient;
        private readonly IPushOverNotificationService _pushOverNotificationService;
        public KucoinStopLossExecuteMarketRule(
            IKucoinClient kucoinClient,
            IPushOverNotificationService pushOverNotificationService)
        {
            _kucoinClient = kucoinClient;
            _pushOverNotificationService = pushOverNotificationService;
        }
        public IRuleResult RuleExecuted(Solbot solbot)
        {
            var result = false;
            var message = string.Empty;

            if (solbot.Communication.StopLoss.IsReady)
            {
                var stopLossOrderResult = _kucoinClient.PlaceOrder(
                        solbot.Strategy.AvailableStrategy.Symbol,
                        Kucoin.Net.Objects.KucoinOrderSide.Sell,
                        Kucoin.Net.Objects.KucoinNewOrderType.Market,
                        quantity: solbot.Communication.StopLoss.AvailableFund);

                if (!(stopLossOrderResult is null))
                {
                    result = stopLossOrderResult.Success;

                    if (stopLossOrderResult.Success)
                    {
                        Logger.Info(LogGenerator.TradeResultStart(stopLossOrderResult.Data.OrderId));

                        var order = _kucoinClient.GetOrder(stopLossOrderResult.Data.OrderId);

                        if (order.Success)
                        {
                            Logger.Info(LogGenerator.TradeResultKucoin(MarketOrder, order.Data, 0.1m));
                        }
                        else
                            Logger.Warn(order.Error.Message);

                        solbot.Actions.BoughtPrice = 0;
                        solbot.Actions.StopLossReached = true;

                        Logger.Info(LogGenerator.TradeResultEndKucoin(stopLossOrderResult.Data.OrderId));

                        _pushOverNotificationService.Send(
                            LogGenerator.NotificationTitle(EnvironmentType.PRODUCTION, MarketOrder, solbot.Strategy.AvailableStrategy.Symbol),
                            LogGenerator.NotificationMessage(
                                solbot.Communication.Average.Current,
                                solbot.Communication.Price.Current,
                                solbot.Communication.Buy.Change));
                    }
                }
                else
                    Logger.Warn(stopLossOrderResult.Error.Message);
            }

            return new MarketRuleResult()
            {
                Success = result,
                Message = result
                    ? LogGenerator.OrderMarketSuccess(MarketOrder)
                    : LogGenerator.OrderMarketError(MarketOrder, message)
            };
        }
    }
}