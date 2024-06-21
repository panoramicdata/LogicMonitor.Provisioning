namespace LogicMonitor.Provisioning.Extensions;

internal static class NcalcExtensions
{
	internal static List<SimpleProperty> Evaluate(this Dictionary<string, string> properties, Dictionary<string, object?> variableDictionary)
	{
		var newProperties = new List<SimpleProperty>();
		foreach (var kvp in properties)
		{
			newProperties.Add(new SimpleProperty
			{
				Name = kvp.Key,
				Value = kvp.Value.Evaluate(variableDictionary)?.ToString() ?? string.Empty
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
								netscanCreationDto.DiscoveredDeviceRule.ChangeName = (string)value;
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
								netscanCreationDto.DiscoveredDeviceRule.Assignment[0].DeviceGroupId = deviceGroup.Id;
								netscanCreationDto.DiscoveredDeviceRule.Assignment[0].GroupName = deviceGroup.FullPath;
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
								netscanCreationDto.DiscoveredDeviceRule.Assignment[0].Type = Enum.TryParse<NetscanAssignmentType>(
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
					case "SubnetScanRange":
						switch (item)
						{
							case NetscanCreationDto netscanCreationDto:
								Prep(netscanCreationDto);
								var ipRangeString = (string)value;
								netscanCreationDto.SubnetScanRange = ipRangeString.Contains('/') ? GetIpRangeFromCidr(ipRangeString) : ipRangeString;
								break;
							default:
								throw new NotSupportedException($"SubnetScanRange can only be set on a {nameof(NetscanCreationDto)}");
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

			if (item is NetscanCreationDto netscanCreationDto2)
			{
				if (netscanCreationDto2.Schedule.Type == NetscanScheduleType.Unknown)
				{
					netscanCreationDto2.Schedule.Type = NetscanScheduleType.Manual;
				}
			}

			list.Add(item);
		}

		return list;
	}

	/// <summary>
	/// Takes input in the form of a CIDR and returns the IP range
	/// Example: 10.38.1.0/24 converts to 10.38.1.1-10.38.1.254
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	private static string GetIpRangeFromCidr(string cidr)
	{
		var parts = cidr.Split('/');
		if (parts.Length != 2)
		{
			throw new ConfigurationException($"Invalid CIDR {cidr}");
		}

		var ip = parts[0];
		var maskBits = int.Parse(parts[1]);
		var ipParts = ip.Split('.');
		if (ipParts.Length != 4)
		{
			throw new ConfigurationException($"Invalid IP {ip}");
		}

		var ipnum = (Convert.ToUInt32(ipParts[0]) << 24) |
			(Convert.ToUInt32(ipParts[1]) << 16) |
			(Convert.ToUInt32(ipParts[2]) << 8) |
			Convert.ToUInt32(ipParts[3]);

		var maskUint = 0xffffffff << (32 - maskBits);

		var startIp = (ipnum & maskUint) + 1;
		var endIp = (ipnum | (maskUint ^ 0xffffffff)) - 1;

		return $"{ToIp(startIp)}-{ToIp(endIp)}";
	}

	private static string ToIp(uint ip) => $"{ip >> 24}.{(ip >> 16) & 0xff}.{(ip >> 8) & 0xff}.{ip & 0xff}";

	private static void Prep(NetscanCreationDto netscanCreationDto)
	{
		netscanCreationDto.Credentials ??= new();

		netscanCreationDto.DiscoveredDeviceRule ??= new();

		if (netscanCreationDto.DiscoveredDeviceRule.Assignment is null)
		{
			netscanCreationDto.DiscoveredDeviceRule.Assignment =
				[
					new() {
						DisableAlerting = false,
						InclusionType = NetscanInclusionType.Include,
						Query = string.Empty
					}
				];
		}

		netscanCreationDto.Schedule ??= new NetscanSchedule
		{
			Notify = false,
			Type = NetscanScheduleType.Manual,
			Recipients = [],
			Cron = string.Empty,
			TimeZone = "America/New_York"
		};

		netscanCreationDto.DuplicatesStrategy ??= new NetscanDuplicatesStrategy
		{
			Type = NetscanExcludeDuplicatesStrategy.MatchingAnyMonitoredDevices,
			Groups = [],
			Collectors = []
		};

		netscanCreationDto.Ports ??= new NetscanPorts
		{
			IsGlobalDefault = true,
			Value = "21,22,23,25,53,69,80,81,110,123,135,143,389,443,445,631,993,1433,1521,3306,3389,5432,5672,6081,7199,8000,8080,8081,9100,10000,11211,27017"
		};
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
