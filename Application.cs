using LogicMonitor.Api;
using LogicMonitor.Api.Collectors;
using LogicMonitor.Api.Dashboards;
using LogicMonitor.Api.Devices;
using LogicMonitor.Api.Filters;
using LogicMonitor.Api.Netscans;
using LogicMonitor.Api.Reports;
using LogicMonitor.Api.Websites;
using LogicMonitor.Provisioning.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogicMonitor.Provisioning
{
	/// <summary>
	/// The main application
	/// </summary>
	internal class Application
	{
		/// <summary>
		/// Configuration
		/// </summary>
		private readonly Configuration _config;

		/// <summary>
		/// The PortalClient to use for LogicMonitor interaction
		/// </summary>
		private readonly PortalClient _portalClient;

		/// <summary>
		/// The logger
		/// </summary>
		private readonly ILogger<Application> _logger;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options"></param>
		/// <param name="logger"></param>
		public Application(
			IOptions<Configuration> options,
			ILogger<Application> logger)
		{
			// Store the config
			_config = options.Value;

			// Validate the credentials
			_config.LogicMonitorCredentials.Validate();

			// Create a portal client
			_portalClient = new PortalClient(
				_config.LogicMonitorCredentials.Account,
				_config.LogicMonitorCredentials.AccessId,
				_config.LogicMonitorCredentials.AccessKey
			);

			// Create a logger
			_logger = logger;
		}

		public async Task Run()
		{
			// Use _logger for logging
			_logger.LogInformation($"Application start.  Mode: {_config.Mode}");

			try
			{
				await ProcessDevicesAsync(_portalClient, _config.Mode, _config.Devices, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessWebsitesAsync(_portalClient, _config.Mode, _config.Websites, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessNetscansAsync(_portalClient, _config.Mode, _config.Netscans, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessDashboardsAsync(_portalClient, _config.Mode, _config.Dashboards, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessReportsAsync(_portalClient, _config.Mode, _config.Reports, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessCollectorsAsync(_portalClient, _config.Mode, _config.Collectors, _config.Customer, _logger).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Exiting due to {e}");
			}
		}

		private static async Task ProcessCollectorsAsync(PortalClient portalClient, Mode mode, Collectors collectors, Customer customer, ILogger<Application> logger)
		{
			if (!collectors.Enabled)
			{
				logger.LogInformation("Not processing collectors, as they are disabled.");
				return;
			}

			// Get any existing CollectorGroup
			var existingCollectorGroup = await portalClient
				.GetAllAsync(new Filter<CollectorGroup>
				{
					FilterItems = new List<FilterItem<CollectorGroup>>
					{
						new Eq<CollectorGroup>(nameof(CollectorGroup.Name), customer.Name)
					}
				})
				.ConfigureAwait(false);

			switch (mode)
			{
				case Mode.Create:
					throw new NotImplementedException();
				case Mode.Delete:
					throw new NotImplementedException();
				default:
					throw new ConfigurationException($"Unexpected mode: {mode}");
			}
		}

		private static async Task ProcessReportsAsync(
			PortalClient portalClient,
			Mode mode,
			Reports reports,
			Customer customer,
			ILogger<Application> logger)
		{
			if (!reports.Enabled)
			{
				logger.LogInformation("Not processing reports, as they are disabled.");
				return;
			}

			// Get any existing ReportGroup
			var existingReportGroup = await portalClient
				.GetAllAsync(new Filter<ReportGroup>
				{
					FilterItems = new List<FilterItem<ReportGroup>>
					{
						new Eq<ReportGroup>(nameof(ReportGroup.Name), customer.Name)
					}
				})
				.ConfigureAwait(false);

			switch (mode)
			{
				case Mode.Create:
					throw new NotImplementedException();
				case Mode.Delete:
					throw new NotImplementedException();
				default:
					throw new ConfigurationException($"Unexpected mode: {mode}");
			}
		}

		private static async Task ProcessDashboardsAsync(
			PortalClient portalClient,
			Mode mode,
			StructureSpecification<DashboardConfig> dashboards,
			Customer customer,
			ILogger<Application> logger)
		{
			if (!dashboards.Enabled)
			{
				logger.LogInformation("Not processing dashboards, as they are disabled.");
				return;
			}

			// Get any existing DashboardGroup
			var existingDashboardGroup = await portalClient
				.GetAllAsync(new Filter<DashboardGroup>
				{
					FilterItems = new List<FilterItem<DashboardGroup>>
					{
						new Eq<DashboardGroup>(nameof(DashboardGroup.Name), $"{dashboards.Root}/{customer.Name}")
					}
				})
				.ConfigureAwait(false);

			switch (mode)
			{
				case Mode.Create:
					throw new NotImplementedException();
				case Mode.Delete:
					throw new NotImplementedException();
				default:
					throw new ConfigurationException($"Unexpected mode: {mode}");
			}
		}

		private static async Task ProcessNetscansAsync(
			PortalClient portalClient,
			Mode mode,
			Netscans netscans,
			Customer customer,
			ILogger<Application> logger)
		{
			if (!netscans.Enabled)
			{
				logger.LogInformation("Not processing netscans, as they are disabled.");
				return;
			}

			// Get any existing NetscanGroup
			var existingNetscanGroup = await portalClient
				.GetAllAsync(new Filter<NetscanGroup>
				{
					FilterItems = new List<FilterItem<NetscanGroup>>
					{
						new Eq<NetscanGroup>(nameof(NetscanGroup.Name), customer.Name)
					}
				})
				.ConfigureAwait(false);

			switch (mode)
			{
				case Mode.Create:
					throw new NotImplementedException();
				case Mode.Delete:
					throw new NotImplementedException();
				default:
					throw new ConfigurationException($"Unexpected mode: {mode}");
			}
		}

		private static async Task ProcessWebsitesAsync(
			PortalClient portalClient,
			Mode mode,
			StructureSpecification<WebsiteConfig> websites,
			Customer customer,
			ILogger<Application> logger)
		{
			if (!websites.Enabled)
			{
				logger.LogInformation("Not processing websites, as they are disabled.");
				return;
			}

			// Get any existing WebsiteGroup
			var existingWebsiteGroup = await portalClient
				.GetAllAsync(new Filter<WebsiteGroup>
				{
					FilterItems = new List<FilterItem<WebsiteGroup>>
					{
						new Eq<WebsiteGroup>(nameof(WebsiteGroup.Name), $"{websites.Root}/{customer.Name}")
					}
				})
				.ConfigureAwait(false);

			switch (mode)
			{
				case Mode.Create:
					throw new NotImplementedException();
				case Mode.Delete:
					throw new NotImplementedException();
				default:
					throw new ConfigurationException($"Unexpected mode: {mode}");
			}
		}

		private static async Task ProcessDevicesAsync(
			PortalClient portalClient,
			Mode mode,
			StructureSpecification<DeviceConfig> devices,
			Customer customer,
			ILogger<Application> logger)
		{
			if (!devices.Enabled)
			{
				logger.LogInformation("Not processing devices, as they are disabled.");
				return;
			}

			// Get any existing DeviceGroup
			var existingDeviceGroup = await portalClient
				.GetAllAsync(new Filter<DeviceGroup>
				{
					FilterItems = new List<FilterItem<DeviceGroup>>
					{
						new Eq<DeviceGroup>(nameof(DeviceGroup.Name), $"{devices.Root}/{customer.Name}")
					}
				})
				.ConfigureAwait(false);

			switch (mode)
			{
				case Mode.Create:
					throw new NotImplementedException();
				case Mode.Delete:
					throw new NotImplementedException();
				default:
					throw new ConfigurationException($"Unexpected mode: {mode}");
			}
		}
	}
}