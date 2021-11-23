namespace LogicMonitor.Provisioning.Extensions;

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

	internal static Dictionary<string, object?> Update(this Dictionary<string, object?> original, Dictionary<string, object?> additional)
	{
		var result = new Dictionary<string, object?>();
		foreach (var kvp in original)
		{
			result[kvp.Key] = kvp.Value;
		}
		foreach (var kvp in additional)
		{
			result[kvp.Key] = kvp.Value;
		}
		return result;
	}

	internal static async Task<List<TOut>> EvaluateAsync<TOut>(
		this IEnumerable<Extended<object>> inputObjects,
		ItemSpec itemSpec,
		LogicMonitorClient logicMonitorClient,
		Dictionary<string, object?> variables,
		CancellationToken cancellationToken) where TOut : class, new()
	{
		var list = new List<TOut>();
		foreach (var inputObject in inputObjects)
		{
			var item = new TOut();
			var tOutPropertyInfos = typeof(TOut).GetProperties();
			var updatedVariables = variables.Update(inputObject.Properties.ToDictionary(kvp => kvp.Key.ToPascalCase(), kvp => kvp.Value));
			foreach (var fieldExpressionKvp in itemSpec.Fields)
			{
				var propertyName = fieldExpressionKvp.Key;
				var valueExpression = fieldExpressionKvp.Value;
				var typeName = typeof(TOut).Name;
				var value = valueExpression.Evaluate(updatedVariables);

				switch (propertyName)
				{
					case "Credentials.DeviceGroupId":
						switch (item)
						{
							case NetscanCreationDto netscanCreationDto:
								Prep(netscanCreationDto);
								netscanCreationDto.Credentials.DeviceGroupId = (int)value;
								netscanCreationDto.Credentials.Custom = new List<object>().ToArray();
								netscanCreationDto.Credentials.DeviceGroupName = (await logicMonitorClient.GetAsync<DeviceGroup>((int)value, cancellationToken).ConfigureAwait(false)).FullPath;
								break;
							default:
								throw new NotSupportedException($"Credentials.DeviceGroupId can only be set on a {nameof(NetscanCreationDto)}");
						}
						break;
					case "Ddr.ChangeName":
						switch (item)
						{
							case NetscanCreationDto netscanCreationDto:
								Prep(netscanCreationDto);
								netscanCreationDto.Ddr.ChangeName = (string)value;
								break;
							default:
								throw new NotSupportedException($"Ddr.ChangeName can only be set on a {nameof(NetscanCreationDto)}");
						}
						break;
					case "Ddr.Assignment[0].DeviceGroupName":
						switch (item)
						{
							case NetscanCreationDto netscanCreationDto:
								Prep(netscanCreationDto);
								var deviceGroup = (await logicMonitorClient
									.GetDeviceGroupByFullPathAsync((string)value, cancellationToken)
									.ConfigureAwait(false)) ?? throw new ConfigurationException($"No such device group '{value}'");
								netscanCreationDto.Ddr.Assignment[0].DeviceGroupId = deviceGroup.Id;
								netscanCreationDto.Ddr.Assignment[0].DeviceGroupName = deviceGroup.FullPath;
								break;
							default:
								throw new NotSupportedException($"Ddr.ChangeName can only be set on a {nameof(NetscanCreationDto)}");
						}
						break;
					case "Ddr.Assignment[0].Type":
						switch (item)
						{
							case NetscanCreationDto netscanCreationDto:
								Prep(netscanCreationDto);
								netscanCreationDto.Ddr.Assignment[0].Type = Enum.TryParse<NetscanAssignmentType>(
									(string)value,
									out var netscanAssignmentType
								)
									? netscanAssignmentType
									: throw new ConfigurationException($"Netscan Assignment type {value} could not be parsed.");
								break;
							default:
								throw new NotSupportedException($"Ddr.ChangeName can only be set on a {nameof(NetscanCreationDto)}");
						}
						break;
					default:
						var propertyInfo = tOutPropertyInfos.SingleOrDefault(pi => pi.Name == propertyName)
							?? throw new ConfigurationException($"Could not find configured property {typeName}.{propertyName}");
						var propertyType = propertyInfo.PropertyType;
						var valueType = value.GetType();
						if (valueType == propertyType)
						{
							propertyInfo.SetValue(item, value);
						}
						else
						{
							if (propertyType.IsEnum && value is string valueString && Enum.TryParse(propertyType, valueString, out var resultEnum))
							{
								propertyInfo.SetValue(item, resultEnum);
							}
							else
							{
								throw new ConfigurationException($"'{valueExpression}' evaluated to a {value.GetType().Name} ({value}) when setting {typeName}.{propertyName}, which is a '{propertyType.Name}'");
							}
						}
						break;
				}
			}
			list.Add(item);
		}
		return list;
	}

	private static void Prep(NetscanCreationDto netscanCreationDto)
	{
		if (netscanCreationDto.Credentials is null)
		{
			netscanCreationDto.Credentials = new();
		}

		if (netscanCreationDto.Ddr is null)
		{
			netscanCreationDto.Ddr = new();
		}

		if (netscanCreationDto.Ddr.Assignment is null)
		{
			netscanCreationDto.Ddr.Assignment = new List<NetscanAssignment>
				{
					new NetscanAssignment
					{
						DisableAlerting = false,
						InclusionType = NetscanInclusionType.Include,
						Query = string.Empty
					}
				};
		}

		if (netscanCreationDto.Schedule is null)
		{
			netscanCreationDto.Schedule = new NetscanSchedule
			{
				Notify = false,
				Type = NetscanScheduleType.Manual,
				Recipients = new(),
				Cron = string.Empty,
				TimeZone = "America/New_York"
			};
		}

		if (netscanCreationDto.DuplicatesStrategy is null)
		{
			netscanCreationDto.DuplicatesStrategy = new NetscanDuplicatesStrategy
			{
				Type = NetscanExcludeDuplicatesStrategy.MatchingAnyMonitoredDevices,
				Groups = new(),
				Collectors = new()
			};
		}

		if (netscanCreationDto.Ports is null)
		{
			netscanCreationDto.Ports = new NetscanPorts
			{
				IsGlobalDefault = true,
				Value = "21,22,23,25,53,69,80,81,110,123,135,143,389,443,445,631,993,1433,1521,3306,3389,5432,5672,6081,7199,8000,8080,8081,9100,10000,11211,27017"
			};
		}
	}

	internal static bool TryEvaluate<T>(
		this Dictionary<string, string> properties,
		string fieldName,
		Dictionary<string, object?> variableDictionary,
		out T outputValue
		)
	{
		if (!properties.TryGetValue(fieldName, out var expression))
		{
			outputValue = default!;
			return false;
		}
		// The expression is available

		var evaluatedValue = expression.Evaluate(variableDictionary);

		if (evaluatedValue is not T evaluatedValueAsT)
		{
			outputValue = default!;
			return false;
		}

		outputValue = evaluatedValueAsT;
		return true;
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
