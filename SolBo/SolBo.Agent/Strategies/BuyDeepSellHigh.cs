﻿using SolBo.Shared.Domain.Enums;
using System.Text.Json.Serialization;

namespace SolBo.Agent.Strategies
{
    public class BuyDeepSellHigh : BaseJob
    {
        public decimal BuyDown { get; set; }
        public decimal SellUp { get; set; }
        public int Average { get; set; }
        public decimal StopLossDown { get; set; }
        public decimal FundPercentage { get; set; }
        public bool ClearOnStartup { get; set; }
        public int StopLossPauseCycles { get; set; }
        public AverageType AverageType { get; set; }
        public SellType SellType { get; set; }
        public CommissionType CommissionType { get; set; }

        [JsonIgnore]
        public bool IsStopLossOn
            => StopLossDown > 0;
    }
}