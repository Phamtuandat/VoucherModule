using JsonLogic.Net;
using Newtonsoft.Json.Linq;

namespace BuildingBlocks.RuleEngine
{
    public class DefaultOperators : IManageOperators
    {
        private readonly Dictionary<string, Func<IProcessJsonLogic, JToken[], object, object>> _operators;

        public DefaultOperators()
        {
            _operators = new Dictionary<string, Func<IProcessJsonLogic, JToken[], object, object>>
            {
                ["=="] = (logic, args, data) =>
                {
                    var left = logic.Apply(args[0], data);
                    var right = logic.Apply(args[1], data);
                    return JToken.DeepEquals(JToken.FromObject(left), JToken.FromObject(right));
                },
                [">"] = (logic, args, data) =>
                {
                    var left = Convert.ToDouble(logic.Apply(args[0], data));
                    var right = Convert.ToDouble(logic.Apply(args[1], data));
                    return left > right;
                },
                ["and"] = (logic, args, data) =>
                {
                    return args.All(arg => Convert.ToBoolean(logic.Apply(arg, data)));
                }
                // Add more as needed
            };
        }

        public void AddOperator(string name, Func<IProcessJsonLogic, JToken[], object, object> operation)
            => _operators[name] = operation;

        public void DeleteOperator(string name)
            => _operators.Remove(name);

        public Func<IProcessJsonLogic, JToken[], object, object> GetOperator(string name)
            => _operators.TryGetValue(name, out var op)
                ? op
                : throw new InvalidOperationException($"Operator '{name}' is not defined.");
    }
}
