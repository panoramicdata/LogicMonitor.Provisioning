namespace LogicMonitor.Provisioning.Config
{
	/// <summary>
	/// A role configuration
	/// </summary>
	public class RoleConfiguration
	{
		/// <summary>
		/// The name
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// The description
		/// </summary>
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// Whether it is enabled.
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// The role privilege operation to apply to all groups
		/// Options: None, Read or Write
		/// </summary>
		public string AccessLevel { get; set; } = "None";

		/// <summary>
		/// The custom help label
		/// </summary>
		public string CustomHelpLabel { get; set; } = string.Empty;

		/// <summary>
		/// The custom help URL
		/// </summary>
		public string CustomHelpUrl { get; set; } = string.Empty;

		/// <summary>
		/// Whether users are required to approve the EULA
		/// </summary>
		public bool IsEulaRequired { get; set; }

		/// <summary>
		/// Whether users are required to use two-factor authentication
		/// </summary>
		public bool IsTwoFactorAuthenticationRequired { get; set; }
	}
}