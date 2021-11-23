namespace LogicMonitor.Provisioning;

internal class FileAndSheetInfo
{
	public FileAndSheetInfo(string evaluatedConfig)
	{
		var configDetails = evaluatedConfig.Split('|') ?? throw new InvalidOperationException("Config should have been validated.  Found missing Repetition.Config");
		if (configDetails.Length != 2)
		{
			throw new ConfigurationException("Repetition config for file should be in the form 'filename|Sheetname'.");
		}
		FileInfo = new FileInfo(configDetails[0]);
		SheetName = configDetails[1];
		// We have the config details

		if (!FileInfo.Exists)
		{
			throw new ConfigurationException($"Repetition config for file could not be found: '{FileInfo.FullName}'.");
		}
	}

	public FileInfo FileInfo { get; set; }
	public string SheetName { get; set; }
}