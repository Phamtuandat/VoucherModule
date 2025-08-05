using JsonLogic.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.RuleEngine;

public static class RuleDebugger
{
    public static void Debug(string ruleJson, object context)
    {
        var evaluator = new JsonLogicEvaluator(new DefaultOperators());

        Console.WriteLine("🧠 RuleDebugger Started -------------------------");
        Console.WriteLine($"📌 Rule: {ruleJson}");
        Console.WriteLine($"📦 Context: {JsonConvert.SerializeObject(context, Formatting.Indented)}");

        try
        {
            // Attempt to evaluate full rule
            var result = evaluator.Apply(ruleJson, context);
            Console.WriteLine($"✅ Final Evaluation Result: {result} (Type: {result?.GetType().Name})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error evaluating rule: {ex.Message}");
        }

        Console.WriteLine("🔍 Extracting individual `var` references...");
        var variables = ExtractVariablesFromRule(ruleJson);

        foreach (var variable in variables)
        {
            try
            {
                var singleVarJson = $"{{ \"var\": \"{variable}\" }}";
                var value = evaluator.Apply(singleVarJson, context);
                Console.WriteLine($"🔑 {variable} = {value} (Type: {value?.GetType().Name})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error extracting variable '{variable}': {ex.Message}");
            }
        }

        Console.WriteLine("🧠 RuleDebugger Finished ------------------------");
    }

    // Helper to extract all "var" references from the rule JSON
    private static List<string> ExtractVariablesFromRule(string ruleJson)
    {
        var variables = new List<string>();

        try
        {
            var jToken = JToken.Parse(ruleJson);
            FindVariables(jToken, variables);
        }
        catch
        {
            // Ignore malformed rule
        }

        return variables.Distinct().ToList();
    }

    private static void FindVariables(JToken token, List<string> vars)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            foreach (var property in obj.Properties())
            {
                if (property.Name == "var" && property.Value.Type == JTokenType.String)
                {
                    vars.Add(property.Value.ToString());
                }
                else
                {
                    FindVariables(property.Value, vars);
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                FindVariables(item, vars);
            }
        }
    }
}