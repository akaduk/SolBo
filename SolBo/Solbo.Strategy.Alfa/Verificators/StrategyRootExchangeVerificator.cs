﻿using Solbo.Strategy.Alfa.Models;
using Solbo.Strategy.Alfa.Rules;
using Solbo.Strategy.Alfa.Validators;
using SolBo.Shared.Strategies.Predefined.Results;

namespace Solbo.Strategy.Alfa.Verificators
{
    internal class StrategyRootExchangeVerificator : IAlfaRule
    {
        public IRuleResult Result(StrategyRootModel strategyRootModel)
        {
            var validator = new StrategyRootExchangeValidator();
            return new RuleResult(validator.Validate(strategyRootModel.Exchange));
        }
    }
}