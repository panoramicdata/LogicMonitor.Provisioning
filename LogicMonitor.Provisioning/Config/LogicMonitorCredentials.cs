namespace LogicMonitor.Provisioning.Config
{
	/// <summary>
	/// LogicMonitor credentials
	/// </summary>
	public class LogicMonitorCredentials
	{
		/// <summary>
		/// The LogicMonitor account
		/// For example, if your URL is https://example.logicmonitor.com/
		/// ... set to "example"
		/// </summary>
		public string Account { get; set; } = string.Empty;

		/// <summary>
		/// The access Id.
		/// See https://www.logicmonitor.com/support/settings/users-and-roles/api-tokens/
		/// </summary>
		public string AccessId { get; set; } = string.Empty;

		/// <summary>
		/// The access key.
		/// See https://www.logicmonitor.com/support/settings/users-and-roles/api-tokens/
		/// </summary>
		public string AccessKey { get; set; } = string.Empty;
	}
}