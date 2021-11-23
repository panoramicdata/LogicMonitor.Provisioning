namespace LogicMonitor.Provisioning.Config;

public enum ItemSpecType
{
	/// <summary>
	/// The single item is specified entirely in configuration
	/// </summary>
	ConfigSingle,

	/// <summary>
	/// The single items is cloned
	/// </summary>
	CloneSingleFromId,

	/// <summary>
	/// Multiple items are imported from spreadsheet using templating
	/// </summary>
	XlsxMulti
}
