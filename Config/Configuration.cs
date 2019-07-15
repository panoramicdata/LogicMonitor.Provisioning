namespace LogicMonitor.Provisioning.Config
{
	/// <summary>
	/// Application configuration, loaded from an appsettings.json file upon execution
	/// You can modify/extend this class and provide your own settings
	/// </summary>
	internal class Configuration
	{
		/// <summary>
		/// LogicMonitor credentials
		/// </summary>
		public LogicMonitorCredentials LogicMonitorCredentials { get; set; }
		public Mode Mode { get; set; }
		public Customer Customer { get; set; }
		public Reports Reports { get; set; }
		public Netscans Netscans { get; set; }
		public Collectors Collectors { get; set; }
		public StructureSpecification<DeviceConfig> Devices { get; set; }
		public StructureSpecification<WebsiteConfig> Websites { get; set; }
		public StructureSpecification<DashboardConfig> Dashboards { get; set; }
	}
}
