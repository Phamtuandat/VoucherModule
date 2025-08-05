using JsonLogic.Net;
using Newtonsoft.Json.Linq;

namespace BuildingBlocks.RuleEngine;

public static class RuleEvaluator
{
    private static readonly JsonLogicEvaluator Evaluator = new(new DefaultOperators());

    public static bool Evaluate(string ruleJson, object dataContext)
    {
        var result = Evaluator.Apply(JToken.Parse(ruleJson), dataContext);
        return result is bool boolean && boolean;
    }
}
