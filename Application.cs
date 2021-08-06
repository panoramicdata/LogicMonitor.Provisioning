using LogicMonitor.Api;
using LogicMonitor.Api.Collectors;
using LogicMonitor.Api.Dashboards;
using LogicMonitor.Api.Devices;
using LogicMonitor.Api.Filters;
using LogicMonitor.Api.Netscans;
using LogicMonitor.Api.Reports;
using LogicMonitor.Api.Topologies;
using LogicMonitor.Api.Users;
using LogicMonitor.Api.Websites;
using LogicMonitor.Provisioning.Config;
using Microsoft.Extensions.Hosting;
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
	internal class Application : IHostedService
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
		/// The CancellationTokenSource
		/// </summary>
		private CancellationTokenSource? TokenSource { get; set; }

		/// <summary>
		/// The running task.
		/// </summary>
		private Task? Task { get; set; }

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

		public async Task RunAsync(Mode mode, CancellationToken cancellationToken)
		{
			// Use _logger for logging
			_logger.LogInformation($"Application start.  Mode: {mode}");

			while (true)
			{
				while (mode == Mode.Menu)
				{
					Console.WriteLine("(C)reate");
					Console.WriteLine("(D)elete");
					Console.WriteLine("Ctrl+C to exit");
					var modeInput = Console.ReadKey();
					mode = modeInput.Key switch
					{
						ConsoleKey.C => Mode.Create,
						ConsoleKey.D => Mode.Delete,
						_ => Mode.Menu,
					};
				}
				await Process(mode, cancellationToken)
					.ConfigureAwait(false);

				mode = Mode.Menu;
			}
		}

		private async Task Process(
			Mode mode,
			CancellationToken cancellationToken)
		{
			try
			{
				// Collectors
				var collectorGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Collectors,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// NetScans
				var netscanGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Netscans,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Reports
				var reportGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Reports,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Dashboards
				var dashboardGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Dashboards,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Devices
				var deviceGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Devices,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Websites
				var websiteGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Websites,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Roles
				var roleGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Roles,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Users
				var userGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Users,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Topologies
				var topologyGroupId = await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Mappings,
					_config.Variables,
					_config.Properties,
					null,
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Are we creating?
				switch (mode)
				{
					case Mode.Create:
						foreach (var roleConfiguration in _config.RoleConfigurations.Where(rc => rc.IsEnabled))
						{
							// Set up roles
							var operation = Enum.TryParse<RolePrivilegeOperation>(roleConfiguration.AccessLevel, out var rolePrivilegeOperation)
								? rolePrivilegeOperation
								: RolePrivilegeOperation.None;
							var roleCreationDto = new RoleCreationDto
							{
								RequireEULA = roleConfiguration.IsEulaRequired,
								TwoFactorAuthenticationRequired = roleConfiguration.IsTwoFactorAuthenticationRequired,
								Name = Substitute(roleConfiguration.Name, _config.Variables),
								Description = Substitute(roleConfiguration.Description, _config.Variables),
								CustomHelpLabel = Substitute(roleConfiguration.CustomHelpLabel, _config.Variables),
								CustomHelpUrl = Substitute(roleConfiguration.CustomHelpUrl, _config.Variables),
								RoleGroupId = roleGroupId.Value,
								Privileges = new List<RolePrivilege> {
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.DeviceGroup,
										ObjectId = deviceGroupId!.ToString(),
										Operation = operation
									},
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.DashboardGroup,
										ObjectId = dashboardGroupId!.ToString(),
										Operation = operation
									},
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.Map,
										ObjectId = topologyGroupId!.ToString(),
										Operation = operation
									},
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.ReportGroup,
										ObjectId = reportGroupId!.ToString(),
										Operation = operation
									},
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.WebsiteGroup,
										ObjectId = websiteGroupId!.ToString(),
										Operation = operation
									},
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.Setting,
										ObjectId = $"useraccess.admingroup.{userGroupId}",
										Operation = operation
									},
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.Setting,
										ObjectId = $"collectorgroup.{collectorGroupId}",
										Operation = operation
									},
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.Setting,
										ObjectId = $"role.{roleGroupId}",
										Operation = RolePrivilegeOperation.Read
									},
									new RolePrivilege
									{
										ObjectType = PrivilegeObjectType.Help,
										ObjectId = "chat",
										Operation = RolePrivilegeOperation.Read
									}
								},
							};
							await _logicMonitorClient
								.CreateAsync(roleCreationDto, cancellationToken)
								.ConfigureAwait(false);
						}
						break;
				}

				_logger.LogInformation("Complete.");
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Failed due to '{e.Message}'");
			}
		}

		private static async Task<int?> ProcessStructureAsync<TGroup, TItem>(
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
				return null;
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
			// currentGroup is now set to either the existing group, or null

			// What mode are we in?
			switch (mode)
			{
				case Mode.Delete:
					// Is there an existing group?
					if (currentGroup is null)
					{
						// No.  There's nothing to do here.
						return null;
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

					return currentGroup.Id;
				case Mode.Create:
					// Create.
					// Is there an existing group?
					if (currentGroup is null)
					{
						// No.  We need to create one.

						currentGroup = await CreateGroupAsync(
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

					break;
				default:
					// Unexpected mode
					var message = $"Unexpected mode: {mode}";
					logger.LogError(message);
					throw new ConfigurationException(message);
			}

			return currentGroup.Id;
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
			TGroup? parentGroup,
			Structure<TGroup, TItem> structure,
			List<Property> variables,
			List<Property> originalProperties)
			where TGroup : IdentifiedItem, IHasEndpoint, new()
			where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			var name = Substitute(structure.Name, variables);
			var description = Substitute(structure.Description, variables);
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
					Name = name,
					Description = description
				} as CreationDto<TGroup>,
				nameof(DashboardGroup) => new DashboardGroupCreationDto
				{
					ParentId = parentGroup?.Id.ToString() ?? "1",
					Name = name,
					Description = description
				} as CreationDto<TGroup>,
				nameof(DeviceGroup) => new DeviceGroupCreationDto
				{
					ParentId = parentGroup?.Id.ToString() ?? "1",
					Name = name,
					Description = description,
					AppliesTo = Substitute(structure.AppliesTo, variables),
					CustomProperties = properties
				} as CreationDto<TGroup>,
				nameof(TopologyGroup) => new TopologyGroupCreationDto
				{
					Name = name,
					Description = description
				} as CreationDto<TGroup>,
				nameof(NetscanGroup) => new NetscanGroupCreationDto
				{
					Name = name,
					Description = description
				} as CreationDto<TGroup>,
				nameof(ReportGroup) => new ReportGroupCreationDto
				{
					Name = name,
					Description = description
				} as CreationDto<TGroup>,
				nameof(RoleGroup) => new RoleGroupCreationDto
				{
					Name = name,
					Description = description,
				} as CreationDto<TGroup>,
				nameof(UserGroup) => new UserGroupCreationDto
				{
					Name = name,
					Description = description
				} as CreationDto<TGroup>,
				nameof(WebsiteGroup) => new WebsiteGroupCreationDto
				{
					ParentId = parentGroup?.Id.ToString() ?? "1",
					Name = name,
					Description = description,
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
					.ConfigureAwait(false))?.Select(dashboardGroup => dashboardGroup.Id) ?? Enumerable.Empty<int>(),
				DeviceGroup deviceGroup => deviceGroup.Devices?.Select(d => d.Id) ?? Enumerable.Empty<int>(),
				NetscanGroup netscanGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<Netscan>
					{
						FilterItems = new List<FilterItem<Netscan>>
						{
							new Eq<Netscan>(nameof(Netscan.GroupId), netscanGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(netscanGroup => netscanGroup.Id) ?? Enumerable.Empty<int>(),
				TopologyGroup mappingGroup => Enumerable.Empty<int>(), // TODO - Add Topology deletion
				ReportGroup reportGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<Report>
					{
						FilterItems = new List<FilterItem<Report>>
						{
							new Eq<Report>(nameof(Report.GroupId), reportGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(reportGroup => reportGroup.Id) ?? Enumerable.Empty<int>(),
				WebsiteGroup websiteGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<Website>
					{
						FilterItems = new List<FilterItem<Website>>
						{
							new Eq<Website>(nameof(Website.WebsiteGroupId), websiteGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(website => website.Id) ?? Enumerable.Empty<int>(),
				RoleGroup roleGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<Role>
					{
						FilterItems = new List<FilterItem<Role>>
						{
							new Eq<Role>(nameof(Role.RoleGroupId), roleGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(role => role.Id) ?? Enumerable.Empty<int>(),
				// Delete users that are ONLY members of this usergroup
				UserGroup userGroup => (await logicMonitorClient
					.GetAllAsync(new Filter<User>
					{
						FilterItems = new List<FilterItem<User>>
						{
							new Eq<User>(nameof(User.UserGroupIds), userGroup.Id)
						}
					})
					.ConfigureAwait(false))?.Select(user => user.Id) ?? Enumerable.Empty<int>(),
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

		public Task StartAsync(CancellationToken cancellationToken)
		{
			if (Task is not null)
			{
				throw new InvalidOperationException("Task is already running.");
			}
			TokenSource = new CancellationTokenSource();
			Task = RunAsync(_config.Mode, TokenSource.Token);
			return Task.CompletedTask;
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (TokenSource is null || Task is null)
			{
				throw new InvalidOperationException("Task is not running.");
			}
			TokenSource.Cancel();
			await Task.ConfigureAwait(false);
		}
	}
}