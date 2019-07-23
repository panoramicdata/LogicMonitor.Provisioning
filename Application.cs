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
				// TODO - Add Control+C cancellation support
				var cancellationToken = CancellationToken.None;
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Collectors, _config.Variables, _config.Properties, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Netscans, _config.Variables, _config.Properties, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Reports, _config.Variables, _config.Properties, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Dashboards, _config.Variables, _config.Properties, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Devices, _config.Variables, _config.Properties, null, _logger, cancellationToken).ConfigureAwait(false);
				await ProcessStructureAsync(_portalClient, _config.Mode, _config.Websites, _config.Variables, _config.Properties, null, _logger, cancellationToken).ConfigureAwait(false);
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
			List<Property> variables,
			List<Property> properties,
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
			logger.LogInformation($"Processing {typeof(TGroup)}...");

			// Get any existing Groups
			// Filter on the group name
			var filterItems = new List<FilterItem<TGroup>>
			{
				new Eq<TGroup>(nameof(NamedEntity.Name), Substitute(structure.Name, variables))
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
			var currentGroup = (await portalClient
				.GetAllAsync(new Filter<TGroup>
				{
					FilterItems = filterItems
				}, cancellationToken)
				.ConfigureAwait(false)).SingleOrDefault();
			// existingGroup is now set to either the existing group, or null

			// What mode are we in?
			switch (mode)
			{
				case Mode.Delete:
					// Delete
					// Is there an existing group?
					if (currentGroup == null)
					{
						// No.  There's nothing to do here.
						return;
					}
					// There's deletion to be done.

					// Recurse child groups first
					foreach (var childStructure in structure.Groups
						?? Enumerable.Empty<Structure<TGroup, TItem>>())
					{
						await ProcessStructureAsync(
							portalClient,
							mode,
							childStructure,
							variables,
							properties,
							currentGroup,
							logger,
							cancellationToken)
							.ConfigureAwait(false);
					}

					// Delete child nodes
					await DeleteChildNodesAsync<TGroup, TItem>(portalClient, currentGroup)
						.ConfigureAwait(false);

					// Try to delete this node
					await DeleteAsync<TGroup>(portalClient, currentGroup.Id)
						.ConfigureAwait(false);

					return;
				case Mode.Create:
					// Create.
					// Is there an existing group?
					if (currentGroup == null)
					{
						// No.  We need to create one.
						currentGroup = await CreateGroupAsync<TGroup, TItem>(
							portalClient,
							parentGroup,
							structure,
							variables,
							properties)
							.ConfigureAwait(false);

						// Create individual items (e.g. Dashboards)
						foreach (var item in structure.Items ?? Enumerable.Empty<ItemSpec>())
						{
							// Ensure that the name is set
							if (item.Name == null)
							{
								throw new ConfigurationException($"Creating items of type '{typeof(TItem).Name}' requires that the Name property is set.");
							}

							// Create any child items
							switch (typeof(TItem).Name)
							{
								case nameof(Dashboard):
									// Ensure that the name and id are set
									if (item.CloneFromId == null)
									{
										throw new ConfigurationException($"Creating items of type '{typeof(TItem).Name}' requires that the CloneFromId property is set.");
									}

									var originalDashboard = await portalClient
										.GetAsync<Dashboard>(item.CloneFromId.Value, cancellationToken)
										.ConfigureAwait(false);

									var newDashboard = await portalClient
										.CloneAsync(item.CloneFromId.Value,
										new DashboardCloneRequest
										{
											Name = Substitute(item.Name, variables),
											Description = Substitute(item.Description, variables),
											DashboardGroupId = currentGroup.Id,
											WidgetsConfig = originalDashboard.WidgetsConfig,
											WidgetsOrder = originalDashboard.WidgetsOrder
										}, cancellationToken)
										.ConfigureAwait(false);

									// All is well
									break;
								default:
									throw new NotSupportedException($"Creating items of type '{typeof(TItem).Name}' is not yet supported.");
							}
						}
					}
					// We now have a group.  Process child structure
					foreach (var childStructure in structure.Groups
						?? Enumerable.Empty<Structure<TGroup, TItem>>())
					{
						await ProcessStructureAsync(
							portalClient,
							mode,
							childStructure,
							variables,
							properties,
							currentGroup,
							logger,
							cancellationToken)
							.ConfigureAwait(false);
					}

					return;
				default:
					// Unexpected mode
					var message = $"Unexpected mode: {mode}";
					logger.LogError(message);
					throw new ConfigurationException(message);
			}
		}

		private static List<Property> GetProperties<TGroup, TItem>(
			Structure<TGroup, TItem> structure,
			List<Property> variables,
			List<Property> properties)
			=> structure.ApplyProperties ? properties.Select(p => new Property { Name = p.Name, Value = Substitute(p.Value, variables) }).ToList() : null;

		private static string Substitute(string inputString, List<Property> variables)
		{
			foreach (var variable in variables)
			{
				inputString = inputString.Replace($"{{{variable.Name}}}", variable.Value);
			}
			return inputString;
		}

		private static async Task<TGroup> CreateGroupAsync<TGroup, TItem>(
			PortalClient portalClient,
			TGroup parentGroup,
			Structure<TGroup, TItem> structure,
			List<Property> variables,
			List<Property> originalProperties)
			where TGroup : IdentifiedItem, IHasEndpoint, new()
			where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			var name = Substitute(structure.Name, variables);
			var properties = structure.ApplyProperties
				? originalProperties
					.Select(p => new Property
					{
						Name = p.Name,
						Value = Substitute(p.Value, variables)
					})
					.ToList()
				: null;
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
						ParentId = parentGroup?.Id.ToString() ?? "1",
						Name = name
					} as CreationDto<TGroup>;
					break;
				case nameof(DeviceGroup):
					creationDto = new DeviceGroupCreationDto
					{
						ParentId = parentGroup?.Id.ToString() ?? "1",
						Name = name,
						AppliesTo = structure.AppliesTo,
						CustomProperties = properties
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
						ParentId = parentGroup?.Id.ToString() ?? "1",
						Name = name,
						Properties = properties
					} as CreationDto<TGroup>;
					break;
				default:
					throw new NotSupportedException($"Creating {groupTypeName}s not supported.");
			}
			return await portalClient
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
						.ConfigureAwait(false))?.Select(c => c.Id) ?? Enumerable.Empty<int>();
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
						.ConfigureAwait(false))?.Select(r => r.Id) ?? Enumerable.Empty<int>();
					break;
				case DeviceGroup deviceGroup:
					ids = deviceGroup.Devices?.Select(d => d.Id) ?? Enumerable.Empty<int>();
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
						.ConfigureAwait(false))?.Select(r => r.Id) ?? Enumerable.Empty<int>();
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
						.ConfigureAwait(false))?.Select(r => r.Id) ?? Enumerable.Empty<int>();
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
						.ConfigureAwait(false))?.Select(w => w.Id) ?? Enumerable.Empty<int>();
					break;
				default:
					throw new NotSupportedException($"Deleting '{typeof(TGroup).Name}' child items not supported.");
			}

			// We have the list of ids to delete
			foreach (var id in ids)
			{
				await DeleteAsync<TItem>(portalClient, id).ConfigureAwait(false);
			}
		}

		private static Task DeleteAsync<TItem>(PortalClient portalClient, int id)
			where TItem : IdentifiedItem, IHasEndpoint, new()
			=> portalClient.DeleteAsync<TItem>(id);
	}
}