namespace LogicMonitor.Provisioning.Config;

public enum RepetitionType
{
	/// <summary>
	/// No repetition: the default
	/// </summary>
	None,

	/// <summary>
	/// Excel file
	/// Config should be set as: &lt;PATH&gt;/&lt;file.xlsx&gt|&lt;Sheet name&gt;
	/// The sheet should contain a single table
	/// Variables will be set per column and objects will take types based on the cell's contents
	/// </summary>
	Xlsx,

	/// <summary>
	/// Google Drive Xlsx
	/// Config should set the &lt;File Id&gt|&lt;Sheet name&gt;
	/// The file id is the long string in the URL of the file
	/// The sheet should contain a single table
	/// Variables will be set per column and objects will take types based on the cell's contents
	/// </summary>
	GoogleDriveXlsx,
}
