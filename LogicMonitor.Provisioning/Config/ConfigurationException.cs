namespace LogicMonitor.Provisioning.Config;

/// <summary>
/// A configuration exception
/// </summary>
[Serializable]
internal class ConfigurationException : Exception
{
	public ConfigurationException()
	{
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="issues">The issues</param>
	public ConfigurationException(List<ConfigurationIssue> issues)
		=> Issues = issues.AsReadOnly();

	public ConfigurationException(string message) : base(message)
	{
	}

	public ConfigurationException(string message, Exception innerException) : base(message, innerException)
	{
	}

	/// <summary>
	/// The issues
	/// </summary>
	public ReadOnlyCollection<ConfigurationIssue> Issues { get; } = new List<ConfigurationIssue>().AsReadOnly();

	/// <inheritdoc />
	public override string ToString() => $"Configuration issues:\r\n{Issues.Select(i => i.Message + "\r\n")}";
}
