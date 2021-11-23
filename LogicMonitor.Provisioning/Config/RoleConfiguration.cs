namespace LogicMonitor.Provisioning.Config;

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
	public string Condition { get; set; } = "true";

	/// <summary>
	/// The role privilege operation to apply to all groups
	/// Options: None, Read or Write
	/// </summary>
	public RolePrivilegeOperation AccessLevel { get; set; }

	/// <summary>
	/// The custom help label
	/// </summary>
	public string CustomHelpLabel { get; set; } = "''";

	/// <summary>
	/// The custom help URL
	/// </summary>
	public string CustomHelpUrl { get; set; } = "''";

	/// <summary>
	/// Whether users are required to approve the EULA
	/// </summary>
	public string IsEulaRequired { get; set; } = "false";

	/// <summary>
	/// Whether users are required to use two-factor authentication
	/// </summary>
	public string IsTwoFactorAuthenticationRequired { get; set; } = "false";
}
