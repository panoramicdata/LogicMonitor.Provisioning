namespace LogicMonitor.Provisioning;

internal class GoogleSheetInfo
{
	public GoogleSheetInfo(string evaluatedConfig)
	{
		var configDetails = evaluatedConfig.Split('|') ?? throw new InvalidOperationException("Config should have been validated.  Found missing Repetition.Config");
		if (configDetails.Length != 2)
		{
			throw new ConfigurationException("Repetition config for file should be in the form 'SheetId|Sheetname'.");
		}

		SheetId = configDetails[0];
		SheetName = configDetails[1];
	}

	public string SheetId { get; set; }

	public string SheetName { get; set; }
}