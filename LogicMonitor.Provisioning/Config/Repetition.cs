namespace LogicMonitor.Provisioning.Config;

/// <summary>
/// How to repeat
/// </summary>
public class Repetition
{
	/// <summary>
	/// The repetition type
	/// </summary>
	public RepetitionType Type { get; set; }

	/// <summary>
	/// The config.
	/// See each Repetition Type for what this property should contain
	/// </summary>
	public string Config { get; set; } = string.Empty;
}
