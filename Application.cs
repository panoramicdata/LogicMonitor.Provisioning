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
		/// The client to use for LogicMonitor interaction
		/// </summary>
		private readonly LogicMonitorClient _logicMonitorClient;

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
			_logicMonitorClient = new LogicMonitorClient(
				new LogicMonitorClientOptions
				{
					Account = _config.LogicMonitorCredentials.Account,
					AccessId = _config.LogicMonitorCredentials.AccessId,
					AccessKey = _config.LogicMonitorCredentials.AccessKey,
					Logger = logger
				}
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

				// Collectors
				await ProcessStructureAsync(
					_logicMonitorClient,
					_config.Mode,
					_config.Collectors,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// NetScans
				await ProcessStructureAsync(
					_logicMonitorClient,
					_config.Mode,
					_config.Netscans,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Reports
				await ProcessStructureAsync(
					_logicMonitorClient,
					_config.Mode,
					_config.Reports,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Dashboards
				await ProcessStructureAsync(
					_logicMonitorClient,
					_config.Mode,
					_config.Dashboards,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Devices
				await ProcessStructureAsync(
					_logicMonitorClient,
					_config.Mode,
					_config.Devices,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Websites
				await ProcessStructureAsync(
					_logicMonitorClient,
					_config.Mode,
					_config.Websites,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Exiting due to {e}");
			}
		}

		private static async Task ProcessStructureAsync<TGroup, TItem>(
			LogicMonitorClient logicMonitorClient,
			Mode mode,
			Structure<TGroup, TItem> structure,
			List<Property> variables,
			List<Property> properties,
			TGroup? parentGroup,
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

			var currentGroups = await logicMonitorClient
				.GetAllAsync(
					new Filter<TGroup>
					{
						FilterItems = filterItems
					},
					cancellationToken
				)
				.ConfigureAwait(false);
			var currentGroup = currentGroups
				.SingleOrDefault();
			// existingGroup is now set to either the existing group, or null

			// What mode are we in?
			switch (mode)
			{
				case Mode.Delete:
					// Is there an existing group?
					if (currentGroup is null)
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
							logicMonitorClient,
							mode,
							childStructure,
							variables,
							properties,
							currentGroup,
							logger,
							cancellationToken
							)
							.ConfigureAwait(false);
					}

					// Delete child nodes
					await DeleteChildNodesAsync<TGroup, TItem>(logicMonitorClient, currentGroup)
						.ConfigureAwait(false);

					// Try to delete this node
					await DeleteAsync<TGroup>(logicMonitorClient, currentGroup.Id)
						.ConfigureAwait(false);

					return;
				case Mode.Create:
					// Create.
					// Is there an existing group?
					if (currentGroup is null)
					{
						// No.  We need to create one.

						// Make sure we have a parent group defined
						if (parentGroup is null)
						{
							throw new InvalidOperationException($"Parent group is not defined for {typeof(TGroup)}.");
						}

						currentGroup = await CreateGroupAsync<TGroup, TItem>(
							logicMonitorClient,
							parentGroup,
							structure,
							variables,
							properties)
							.ConfigureAwait(false);

						// Create individual items (e.g. Dashboards)
						foreach (var item in structure.Items ?? Enumerable.Empty<ItemSpec>())
						{
							// Ensure that the name is set
							if (item.Name is null)
							{
								throw new ConfigurationException($"Creating items of type '{typeof(TItem).Name}' requires that the Name property is set.");
							}

							// Create any child items
							switch (typeof(TItem).Name)
							{
								case nameof(Dashboard):
									// Ensure that the name and id are set
									if (item.CloneFromId is null)
									{
										throw new ConfigurationException($"Creating items of type '{typeof(TItem).Name}' requires that the CloneFromId property is set.");
									}

									var originalDashboard = await logicMonitorClient
										.GetAsync<Dashboard>(item.CloneFromId.Value, cancellationToken)
										.ConfigureAwait(false);

									var newDashboard = await logicMonitorClient
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
							logicMonitorClient,
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

		//private static List<Property>? GetProperties<TGroup, TItem>(
		//	Structure<TGroup, TItem> structure,
		//	List<Property> variables,
		//	List<Property> properties)
		//	=> structure.ApplyProperties
		//		? properties.ConvertAll(p => new Property { Name = p.Name, Value = Substitute(p.Value, variables) })
		//		: null;

		private static string? Substitute(string? inputString, List<Property> variables)
		{
			if (inputString is null)
			{
				return null;
			}

			foreach (var variable in variables)
			{
				inputString = inputString.Replace($"{{{variable.Name}}}", variable.Value);
			}
			return inputString;
		}

		private static async Task<TGroup> CreateGroupAsync<TGroup, TItem>(
			LogicMonitorClient portalClient,
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
					.ConvertAll(p => new Property
					{
						Name = p.Name,
						Value = Substitute(p.Value, variables)
					})
				: null;
			var groupTypeName = typeof(TGroup).Name;
			var creationDto = groupTypeName switch
			{
				nameof(CollectorGroup) => new CollectorGroupCreationDto
				{
					Name = name
				} as CreationDto<TGroup>,
				nameof(DashboardGroup) => new DashboardGroupCreationDto
				{
					ParentId = parentGroup?.Id.ToString() ?? "1",
					Name = name
				} as CreationDto<TGroup>,
				nameof(DeviceGroup) => new DeviceGroupCreationDto
				{
					ParentId = parentGroup?.Id.ToString() ?? "1",
					Name = name,
					AppliesTo = Substitute(structure.AppliesTo, variables),
					CustomProperties = properties
				} as CreationDto<TGroup>,
				nameof(NetscanGroup) => new NetscanGroupCreationDto
				{
					Name = name
				} as CreationDto<TGroup>,
				nameof(ReportGroup) => new ReportGroupCreationDto
				{
					Name = name
				} as CreationDto<TGroup>,
				nameof(WebsiteGroup) => new WebsiteGroupCreationDto
				{
					ParentId = parentGroup?.Id.ToString() ?? "1",
					Name = name,
					Properties = properties
				} as CreationDto<TGroup>,
				_ => throw new NotSupportedException($"Creating {groupTypeName}s not supported."),
			};
			return await portalClient
				.CreateAsync(creationDto)
				.ConfigureAwait(false);
		}

		private static async Task DeleteChildNodesAsync<TGroup, TItem>(
			LogicMonitorClient logicMonitorClient,
			TGroup group)
			where TGroup : IdentifiedItem, IHasEndpoint, new()
			where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			var ids = group switch
			{
				CollectorGroup collectorGroup => (await logicMonitorClient
					.GetAllCollectorsByCollectorGroupId(collectorGroup.Id)
					.ConfigureAwait(false))?.Select(c => c.Id) ?? Enumerable.Empty<int>(),
				DashboardGroup dashboardGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<Dashboard>
					{
						FilterItems = new List<FilterItem<Dashboard>>
						{
							new Eq<Dashboard>(nameof(Dashboard.DashboardGroupId), dashboardGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(r => r.Id) ?? Enumerable.Empty<int>(),
				DeviceGroup deviceGroup => deviceGroup.Devices?.Select(d => d.Id) ?? Enumerable.Empty<int>(),
				NetscanGroup netscanGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<Netscan>
					{
						FilterItems = new List<FilterItem<Netscan>>
						{
							new Eq<Netscan>(nameof(Netscan.GroupId), netscanGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(r => r.Id) ?? Enumerable.Empty<int>(),
				ReportGroup reportGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<Report>
					{
						FilterItems = new List<FilterItem<Report>>
						{
							new Eq<Report>(nameof(Report.GroupId), reportGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(r => r.Id) ?? Enumerable.Empty<int>(),
				WebsiteGroup websiteGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<Website>
					{
						FilterItems = new List<FilterItem<Website>>
						{
							new Eq<Website>(nameof(Website.WebsiteGroupId), websiteGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(w => w.Id) ?? Enumerable.Empty<int>(),
				_ => throw new NotSupportedException($"Deleting '{typeof(TGroup).Name}' child items not supported."),
			};

			// We have the list of ids to delete
			foreach (var id in ids)
			{
				await DeleteAsync<TItem>(logicMonitorClient, id)
					.ConfigureAwait(false);
			}
		}

		private static Task DeleteAsync<TItem>(LogicMonitorClient logicMonitorClient, int id)
			where TItem : IdentifiedItem, IHasEndpoint, new()
			=> logicMonitorClient.DeleteAsync<TItem>(id);
	}
}