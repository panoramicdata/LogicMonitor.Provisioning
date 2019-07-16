using LogicMonitor.Api;
using LogicMonitor.Api.Collectors;
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
using System.Threading;
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
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Devices, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Websites, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Netscans, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Dashboards, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Reports, _config.Customer, _logger).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Collectors, _config.Customer, _logger).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Exiting due to {e}");
			}
		}

		private static async Task ProcessStructureAsync<TGroup, TItem>(
			PortalClient portalClient,
			Mode mode,
			Structure<TGroup, TItem> structure,
			Customer customer,
			ILogger<Application> logger) where TGroup : IHasEndpoint, new()
		{
			if (!structure.Enabled)
			{
				logger.LogInformation($"Not processing {typeof(TGroup)}, as they are disabled.");
				return;
			}

			// Get any existing Groups
			var filter = new Filter<TGroup>
			{
				FilterItems = new List<FilterItem<TGroup>>
					{
						new Eq<TGroup>(nameof(NamedEntity.Name), customer.Name)
					}
			};
			var cancellationToken = CancellationToken.None;

			var existingCollectorGroup = await portalClient
				.GetAllAsync(filter, cancellationToken)
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

		private static async Task EnsureStructureIntactAsync<TGroup, TItem>(
			PortalClient portalClient,
			Structure<TGroup, TItem> structure,
			Mode mode,
			Customer customer) where TGroup : IHasEndpoint, new()
		{
			// Check supported modes
			switch (mode)
			{
				case Mode.Create:
					break;
				default:
					throw new NotSupportedException($"Mode {mode} not yet supported.");
			}

			string fullPathPropertyName = null;
			switch (typeof(TGroup).Name)
			{
				case nameof(DeviceGroup):
					fullPathPropertyName = nameof(DeviceGroup.FullPath);
					break;
				case nameof(WebsiteGroup):
					fullPathPropertyName = nameof(WebsiteGroup.FullPath);
					break;
				case nameof(CollectorGroup):
					fullPathPropertyName = nameof(CollectorGroup.Name);
					break;
				case nameof(NetscanGroup):
					fullPathPropertyName = nameof(NetscanGroup.Name);
					break;
			}

			// Get any existing DeviceGroup
			var existingGroup = await portalClient
				.GetAllAsync(new Filter<DeviceGroup>
				{
					FilterItems = new List<FilterItem<DeviceGroup>>
					{
						new Eq<DeviceGroup>(fullPathPropertyName, $"{structure.Root}/")
					}
				})
				.ConfigureAwait(false);

			// TODO - Create it if it doesn't exist
			if (existingGroup == null)
			{

			}

			// Iterate all the child groups

			// Iterate all the items
		}
	}
}