namespace LogicMonitor.Provisioning.Config;

/// <summary>
/// Application configuration, loaded from an appsettings.json file upon execution
/// You can modify/extend this class and provide your own settings
/// </summary>
internal class Configuration
{
	/// <summary>
	/// LogicMonitor credentials
	/// </summary>
	public LogicMonitorCredentials LogicMonitorCredentials { get; set; } = new();

	public Repetition Repetition { get; set; } = new();

	/// <summary>
	/// The mode (create = provisioning, delete = removal)
	/// </summary>
	public Mode Mode { get; set; }

	/// <summary>
	/// Variables to set for all groups where properties can apply
	/// </summary>
	public Dictionary<string, string> Variables { get; set; } = [];

	/// <summary>
	/// The Report structure to apply.
	/// Note that LogicMonitor does not support nesting here.
	/// </summary>
	public Structure<ReportGroup, ReportBase>? Reports { get; set; }

	/// <summary>
	/// The NetScan structure to apply
	/// Note that LogicMonitor does not support nesting here.
	/// </summary>
	public Structure<NetscanGroup, Netscan>? Netscans { get; set; }

	/// <summary>
	/// The Collector structure to apply
	/// Note that LogicMonitor does not support nesting here.
	/// </summary>
	public Structure<CollectorGroup, Collector>? Collectors { get; set; }

	/// <summary>
	/// The Resource structure to apply
	/// </summary>
	public Structure<DeviceGroup, Device>? Resources { get; set; }

	/// <summary>
	/// The Website structure to apply
	/// </summary>
	public Structure<WebsiteGroup, Website>? Websites { get; set; }

	/// <summary>
	/// The Dashboard structure to apply
	/// </summary>
	public Structure<DashboardGroup, Dashboard>? Dashboards { get; set; }

	/// <summary>
	/// The Role structure to apply
	/// Note that LogicMonitor does not support nesting here.
	/// </summary>
	public Structure<RoleGroup, Role>? Roles { get; set; }

	/// <summary>
	/// The Topology structure to apply
	/// Note that LogicMonitor does not support nesting here.
	/// </summary>
	public Structure<TopologyGroup, Topology>? Mappings { get; set; }

	/// <summary>
	/// The User structure to apply
	/// Note that LogicMonitor does not support nesting here.
	/// </summary>
	public Structure<UserGroup, User>? Users { get; set; }

	/// <summary>
	/// Role configurations
	/// </summary>
	public List<RoleConfiguration>? RoleConfigurations { get; set; }
}
