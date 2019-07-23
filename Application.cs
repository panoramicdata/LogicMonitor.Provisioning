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
using System.Linq;
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
				// TODO - Add Ctrl+C cancellation support
				var cancellationToken = CancellationToken.None;
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Collectors, _config.Customer, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Netscans, _config.Customer, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Reports, _config.Customer, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Dashboards, _config.Customer, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Devices, _config.Customer, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Websites, _config.Customer, null, _logger, cancellationToken).ConfigureAwait(false);
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
			TGroup parentGroup,
			ILogger<Application> logger,
			CancellationToken cancellationToken)
				where TGroup : IdentifiedItem, IHasEndpoint, new()
				where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			if (!structure.Enabled)
			{
				logger.LogInformation($"Not processing {typeof(TGroup)}, as they are disabled.");
				return;
			}
			// Structure is enabled

			// Get any existing Groups
			// Filter on the group name
			var filterItems = new List<FilterItem<TGroup>>
			{
				new Eq<TGroup>(nameof(NamedEntity.Name), GetName(structure.Name, customer.Name))
			};
			// For hierarchical groups, also filter on the parent id
			switch (typeof(TGroup).Name)
			{
				case nameof(DashboardGroup):
					filterItems.Add(new Eq<TGroup>(nameof(DashboardGroup.ParentId), parentGroup?.Id ?? 1));
					break;
				case nameof(DeviceGroup):
					filterItems.Add(new Eq<TGroup>(nameof(DeviceGroup.ParentId), parentGroup?.Id ?? 1));
					break;
				case nameof(WebsiteGroup):
					filterItems.Add(new Eq<TGroup>(nameof(WebsiteGroup.ParentId), parentGroup?.Id ?? 1));
					break;
			}
			var existingGroup = (await portalClient
				.GetAllAsync(new Filter<TGroup>
				{
					FilterItems = filterItems
				}, cancellationToken)
				.ConfigureAwait(false)).SingleOrDefault();

			switch (mode)
			{
				case Mode.Delete:
					// Is there an existing group?
					if (existingGroup == null)
					{
						// No.  There's nothing to do here.
						return;
					}
					// There's deletion to be done.

					// Recurse child groups first
					foreach (var childStructure in structure.Groups ?? Enumerable.Empty<Structure<TGroup, TItem>>())
					{
						await ProcessStructureAsync(
							portalClient,
							mode,
							childStructure,
							customer,
							existingGroup,
							logger,
							cancellationToken)
							.ConfigureAwait(false);
					}

					// Delete child nodes
					await DeleteChildNodesAsync<TGroup, TItem>(portalClient, existingGroup)
						.ConfigureAwait(false);

					return;
				case Mode.Create:
					// Is there an existing group?
					if (existingGroup == null)
					{
						// No.  We need to create one.
						await CreateGroupAsync<TGroup, TItem>(
							portalClient,
							existingGroup,
							GetName(structure.Name, customer.Name))
							.ConfigureAwait(false);
					}
					// We now have a group.  Process child structure
					foreach (var childStructure in structure.Groups ?? Enumerable.Empty<Structure<TGroup, TItem>>())
					{
						await ProcessStructureAsync(
							portalClient,
							mode,
							childStructure,
							customer,
							existingGroup,
							logger,
							cancellationToken)
							.ConfigureAwait(false);
					}

					return;
				default:
					throw new ConfigurationException($"Unexpected mode: {mode}");
			}
		}

		private static string GetName(string groupName, string customerName)
			=> groupName.Replace("{CustomerName}", customerName);

		private static async Task CreateGroupAsync<TGroup, TItem>(
			PortalClient portalClient,
			TGroup existingGroup,
			string name)
			where TGroup : IdentifiedItem, IHasEndpoint, new()
			where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			var groupTypeName = typeof(TGroup).Name;
			CreationDto<TGroup> creationDto;
			switch (groupTypeName)
			{
				case nameof(CollectorGroup):
					creationDto = new CollectorGroupCreationDto
					{
						Name = name
					} as CreationDto<TGroup>;
					break;
				case nameof(DashboardGroup):
					creationDto = new DashboardGroupCreationDto
					{
						ParentId = existingGroup?.Id.ToString() ?? "1",
						Name = name
					} as CreationDto<TGroup>;
					break;
				case nameof(DeviceGroup):
					creationDto = new DeviceGroupCreationDto
					{
						ParentId = existingGroup?.Id.ToString() ?? "1",
						Name = name
					} as CreationDto<TGroup>;
					break;
				case nameof(NetscanGroup):
					creationDto = new NetscanGroupCreationDto
					{
						Name = name
					} as CreationDto<TGroup>;
					break;
				case nameof(ReportGroup):
					creationDto = new ReportGroupCreationDto
					{
						Name = name
					} as CreationDto<TGroup>;
					break;
				case nameof(WebsiteGroup):
					creationDto = new WebsiteGroupCreationDto
					{
						ParentId = existingGroup?.Id.ToString() ?? "1",
						Name = name
					} as CreationDto<TGroup>;
					break;
				default:
					throw new NotSupportedException($"Creating {groupTypeName}s not supported.");
			}
			await portalClient
				.CreateAsync(creationDto)
				.ConfigureAwait(false);
		}

		private static async Task DeleteChildNodesAsync<TGroup, TItem>(PortalClient portalClient, TGroup group)
			where TGroup : IdentifiedItem, IHasEndpoint, new()
			where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			IEnumerable<int> ids;
			switch (group)
			{
				case CollectorGroup collectorGroup:
					ids = (await portalClient
						.GetAllCollectorsByCollectorGroupId(collectorGroup.Id)
						.ConfigureAwait(false)).Select(c => c.Id);
					break;
				case DashboardGroup dashboardGroup:
					ids = (await portalClient
						.GetAllAsync(new Filter<Dashboard>
						{
							FilterItems = new List<FilterItem<Dashboard>>
							{
								new Eq<Dashboard>(nameof(Dashboard.DashboardGroupId), dashboardGroup.Id)
							}
						})
						.ConfigureAwait(false)).Select(r => r.Id);
					break;
				case DeviceGroup deviceGroup:
					ids = deviceGroup.Devices.Select(d => d.Id);
					break;
				case NetscanGroup netscanGroup:
					ids = (await portalClient
						.GetAllAsync(new Filter<Netscan>
						{
							FilterItems = new List<FilterItem<Netscan>>
							{
								new Eq<Netscan>(nameof(Netscan.GroupId), netscanGroup.Id)
							}
						})
						.ConfigureAwait(false)).Select(r => r.Id);
					break;
				case ReportGroup reportGroup:
					ids = (await portalClient
						.GetAllAsync(new Filter<Report>
						{
							FilterItems = new List<FilterItem<Report>>
							{
								new Eq<Report>(nameof(Report.GroupId), reportGroup.Id)
							}
						})
						.ConfigureAwait(false)).Select(r => r.Id);
					break;
				case WebsiteGroup websiteGroup:
					ids = (await portalClient
						.GetAllAsync(new Filter<Website>
						{
							FilterItems = new List<FilterItem<Website>>
							{
								new Eq<Website>(nameof(Website.WebsiteGroupId), websiteGroup.Id)
							}
						})
						.ConfigureAwait(false)).Select(w => w.Id);
					break;
				default:
					throw new NotSupportedException($"Deleting '{typeof(TGroup).Name}' child items not supported.");
			}

			// We have the list of ids to delete
			foreach (var id in ids)
			{
				await portalClient
					.DeleteAsync<TItem>(id)
					.ConfigureAwait(false);
			}
		}
	}
}