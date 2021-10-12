using LogicMonitor.Api;
using PanoramicData.NCalcExtensions;
using System;
using System.Collections.Generic;

namespace LogicMonitor.Provisioning.Extensions
{
	internal static class NcalcExtensions
	{
		internal static List<Property> Evaluate(this Dictionary<string, string> properties, Dictionary<string, object?> variableDictionary)
		{
			var newProperties = new List<Property>();
			foreach (var kvp in properties)
			{
				newProperties.Add(new Property
				{
					Name = kvp.Key,
					Value = kvp.Value.Evaluate(variableDictionary).ToString()
				});
			}
			return newProperties;
		}

		internal static object Evaluate(this string expressionString, Dictionary<string, object?> variableDictionary)
			=> new ExtendedExpression(expressionString) { Parameters = variableDictionary }.Evaluate();

		internal static T? Evaluate<T>(this string expressionString, Dictionary<string, object?> variableDictionary)
		{
			try
			{
				return (T)new ExtendedExpression(expressionString) { Parameters = variableDictionary }.Evaluate();
			}
			catch (InvalidCastException)
			{
				return default;
			}
		}
	}
}