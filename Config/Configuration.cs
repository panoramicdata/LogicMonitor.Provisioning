using LogicMonitor.Api;
using LogicMonitor.Api.Collectors;
using LogicMonitor.Api.Dashboards;
using LogicMonitor.Api.Devices;
using LogicMonitor.Api.Netscans;
using LogicMonitor.Api.Reports;
using LogicMonitor.Api.Websites;
using System.Collections.Generic;

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
		public List<Property> Variables { get; set; }
		public List<Property> Properties { get; set; }
		public Structure<ReportGroup, Report> Reports { get; set; }
		public Structure<NetscanGroup, Netscan> Netscans { get; set; }
		public Structure<CollectorGroup, Collector> Collectors { get; set; }
		public Structure<DeviceGroup, Device> Devices { get; set; }
		public Structure<WebsiteGroup, Website> Websites { get; set; }
		public Structure<DashboardGroup, Dashboard> Dashboards { get; set; }
	}
}
