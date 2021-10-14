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
using LogicMonitor.Provisioning.Config.Validators;
using LogicMonitor.Provisioning.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PanoramicData.SheetMagic;
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

			var validator = new ConfigurationValidator();
			var validationResult = validator.Validate(_config);
			if (!validationResult.IsValid)
			{
				throw new ArgumentException("Invalid configuation:\n" + string.Join("\n", validationResult.Errors.Select(e => e.ErrorMessage)));
			}

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

		public async Task RunAsync(CancellationToken cancellationToken)
		{
			// Use _logger for logging
			var mode = _config.Mode;
			var repetition = _config.Repetition;
			var variables = new Dictionary<string, object?>();
			foreach (var kvp in _config.Variables)
			{
				variables[kvp.Key] = kvp.Value.Evaluate(variables);
			}
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

				foreach (var repetitionItem in GetRepetitionItems(repetition, variables))
				{
					// Skip this row if the IsEnabled cell is set and
					// - does not parse as a bool or
					// - is set to "FALSE"
					if (
						repetitionItem.TryGetValue("IsEnabled", out var isEnabledValue)
						&& (isEnabledValue is not bool isEnabledBool || !isEnabledBool)
					)
					{
						// Skip this one
						continue;
					}

					await ProcessAsync(
						mode,
						repetitionItem,
						cancellationToken)
						.ConfigureAwait(false);
				}
				mode = Mode.Menu;
			}
		}

		private static List<Dictionary<string, object?>> GetRepetitionItems(
			Repetition repetition,
			Dictionary<string, object?> variables)
		{
			switch (repetition.Type)
			{
				case RepetitionType.None:
					// Is the Config is set
					if (!string.IsNullOrWhiteSpace(repetition.Config))
					{
						// Yes - this is not supported
						throw new ConfigurationException($"Unexpected repetition config for repetition type: '{repetition.Type}'");
					}
					// Return a single item
					return new List<Dictionary<string, object?>>()
				{
					variables
				};
				case RepetitionType.Xlsx:
					{
						// Try to parse the config
						var evaluatedConfig = repetition.Config.Evaluate<string>(variables);
						var fileAndSheetInfo = new FileAndSheetInfo(evaluatedConfig ?? throw new ConfigurationException("Xlsx config should evaluate to a string."));

						using var magicSpreadsheet = new MagicSpreadsheet(fileAndSheetInfo.FileInfo);
						magicSpreadsheet.Load();
						var sheetExtendedObjects = magicSpreadsheet.GetExtendedList<object>(fileAndSheetInfo.SheetName);
						var repetitionItems = new List<Dictionary<string, object?>>();
						foreach (var sheetExtendedObject in sheetExtendedObjects)
						{
							// Construct a dictionary based on the provided variables, plus the spreadsheet items
							var itemDictionary = new Dictionary<string, object?>();
							foreach (var kvp in variables)
							{
								itemDictionary[kvp.Key] = kvp.Value;
							}
							foreach (var kvp in sheetExtendedObject.Properties)
							{
								itemDictionary[kvp.Key.ToPascalCase()] = kvp.Value;
							}
							repetitionItems.Add(itemDictionary);
						}
						return repetitionItems;
					}
				default:
					throw new NotSupportedException($"Unsupported repetition type: '{repetition.Type}'");
			}
		}

		private async Task ProcessAsync(
			Mode mode,
			Dictionary<string, object?> variables,
			CancellationToken cancellationToken)
		{
			try
			{
				// Collectors
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Collectors,
					variables,
					null,
					"collectorGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Devices
				var parentDeviceGroup = _config.Devices.Parent is null
					? null
					: await _logicMonitorClient
						.GetDeviceGroupByFullPathAsync(_config.Devices.Parent.Evaluate<string>(variables), cancellationToken)
						.ConfigureAwait(false);
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Devices,
					variables,
					parentDeviceGroup,
					"deviceGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// NetScans
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Netscans,
					variables,
					null,
					"netscanGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Reports
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Reports,
					variables,
					null,
					"reportGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Dashboards
				var parentDashboardGroup = _config.Dashboards.Parent is null
					? null
					: await _logicMonitorClient
						.GetDashboardGroupByFullPathAsync(_config.Dashboards.Parent.Evaluate<string>(variables), cancellationToken)
						.ConfigureAwait(false);
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Dashboards,
					variables,
					parentDashboardGroup,
					"dashboardGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Websites
				var parentWebsiteGroup = _config.Websites.Parent is null
					? null
					: await _logicMonitorClient
						.GetWebsiteGroupByFullPathAsync(_config.Websites.Parent.Evaluate<string>(variables), cancellationToken)
						.ConfigureAwait(false);
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Websites,
					variables,
					parentWebsiteGroup,
					"websiteGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Roles
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Roles,
					variables,
					null,
					"roleGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Users
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Users,
					variables,
					null,
					"userGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				// Topologies
				await ProcessStructureAsync(
					_logicMonitorClient,
					mode,
					_config.Mappings,
					variables,
					null,
					"topologyGroupId",
					_logger,
					cancellationToken)
					.ConfigureAwait(false);

				await ProcessRoleConfigurationsAsync(
					mode,
					_config.RoleConfigurations,
					variables,
					_logicMonitorClient,
					cancellationToken)
					.ConfigureAwait(false);

				_logger.LogInformation("Complete.");
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Failed due to '{e.Message}'");
			}
		}

		private static async Task ProcessRoleConfigurationsAsync(
			Mode mode,
			List<RoleConfiguration> roleConfigurations,
			Dictionary<string, object?> variables,
			LogicMonitorClient logicMonitorClient,
			CancellationToken cancellationToken)
		{
			// Are we creating?
			switch (mode)
			{
				case Mode.Create:
					var roleGroupId = (variables.TryGetValue("roleGroupId", out var roleGroupIdNullable) ? roleGroupIdNullable as int? : null) ?? 0;
					foreach (var roleConfiguration in roleConfigurations.Where(rc => rc.Condition.Evaluate(variables) as bool? == true))
					{
						// Set up roles
						var roleName = roleConfiguration.Name.Evaluate<string>(variables);

						// Remove any existing role
						var existingRoles = await logicMonitorClient.GetAllAsync<Role>(
							new Filter<Role>
							{
								FilterItems = new List<FilterItem<Role>>
								{
									new Eq<Role>(nameof(Role.Name), roleName),
									new Eq<Role>(nameof(Role.RoleGroupId), roleGroupId),
								}
							}, cancellationToken
							).ConfigureAwait(false);
						if (existingRoles?.Count > 0)
						{
							await logicMonitorClient.DeleteAsync(existingRoles[0], cancellationToken: cancellationToken)
								.ConfigureAwait(false);
						}

						var roleCreationDto = new RoleCreationDto
						{
							RequireEULA = roleConfiguration.IsEulaRequired.Evaluate<bool>(variables),
							TwoFactorAuthenticationRequired = roleConfiguration.IsTwoFactorAuthenticationRequired.Evaluate<bool>(variables),
							Name = roleName,
							Description = roleConfiguration.Description.Evaluate<string>(variables),
							CustomHelpLabel = roleConfiguration.CustomHelpLabel.Evaluate<string>(variables),
							CustomHelpUrl = roleConfiguration.CustomHelpUrl.Evaluate<string>(variables),
							RoleGroupId = roleGroupId,
							Privileges = new(),
						};

						foreach (var x in new List<RoleSpec>
						{
							new (
								PrivilegeObjectType.DeviceGroup,
								variables.TryGetValue("deviceGroupId", out var deviceGroupId) ? deviceGroupId?.ToString() : null,
								roleConfiguration.AccessLevel),
							new (
								PrivilegeObjectType.DashboardGroup,
								variables.TryGetValue("dashboardGroupId", out var dashboardGroupId) ? dashboardGroupId?.ToString() : null,
								roleConfiguration.AccessLevel),
							new (
								PrivilegeObjectType.Map,
								variables.TryGetValue("topologyGroupId", out var topologyGroupId) ? topologyGroupId?.ToString() : null,
								roleConfiguration.AccessLevel),
							new (
								PrivilegeObjectType.ReportGroup,
								variables.TryGetValue("reportGroupId", out var reportGroupId) ? reportGroupId?.ToString() : null,
								roleConfiguration.AccessLevel),
							new (
								PrivilegeObjectType.WebsiteGroup,
								variables.TryGetValue("websiteGroupId", out var websiteGroupId) ? websiteGroupId?.ToString() : null,
								roleConfiguration.AccessLevel),
							new (
								PrivilegeObjectType.Setting,
								variables.TryGetValue("userGroupId", out var userGroupId) ? $"useraccess.admingroup.{userGroupId}" : null,
								roleConfiguration.AccessLevel),
							new (
								PrivilegeObjectType.Setting,
								variables.TryGetValue("collectorGroupId", out var collectorGroupId) ? $"collectorgroup.{collectorGroupId}" : null,
								roleConfiguration.AccessLevel),
							new (
								PrivilegeObjectType.Setting,
								$"role.{roleGroupId}",
								RolePrivilegeOperation.Read),
							new (
								PrivilegeObjectType.Help,
								"chat",
								RolePrivilegeOperation.Read)
						})
						{
							if (x.ObjectId is not string objectId)
							{
								continue;
							}

							// Add the priviledge
							var rolePriviledge = new RolePrivilege
							{
								ObjectType = x.ObjectType,
								ObjectId = objectId,
								Operation = x.RolePrivilegeOperation,
							};
							roleCreationDto.Privileges.Add(rolePriviledge);
						}

						await logicMonitorClient
							.CreateAsync(roleCreationDto, cancellationToken)
							.ConfigureAwait(false);
					}
					break;
			}
		}

		private static async Task ProcessStructureAsync<TGroup, TItem>(
			LogicMonitorClient logicMonitorClient,
			Mode mode,
			Structure<TGroup, TItem> structure,
			Dictionary<string, object?> variables,
			TGroup? parentGroup,
			string groupVariableName,
			ILogger<Application> logger,
			CancellationToken cancellationToken)
				where TGroup : IdentifiedItem, IHasEndpoint, new()
				where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			if (!structure.Condition.Evaluate<bool>(variables))
			{
				logger.LogInformation($"Not processing {typeof(TGroup)}, as they are disabled.");
				return;
			}
			// Structure is enabled
			logger.LogInformation($"Processing {typeof(TGroup)}...");

			// Determine the properties to set
			var properties = structure.Properties;

			// Get any existing Groups
			// Filter on the group name
			var filterItems = new List<FilterItem<TGroup>>
			{
				new Eq<TGroup>(nameof(NamedEntity.Name), structure.Name.Evaluate(variables))
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
							currentGroup,
							groupVariableName,
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

						currentGroup = await CreateGroupAsync(
							logicMonitorClient,
							parentGroup,
							structure,
							variables)
							.ConfigureAwait(false);

						// Create individual items (e.g. Dashboards)
						foreach (var item in structure.Items ?? Enumerable.Empty<ItemSpec>())
						{
							// Ensure that the name is set
							if (!item.Fields.TryEvaluate<string>("Name", variables, out var itemName))
							{
								throw new ConfigurationException($"Creating items of type '{typeof(TItem).Name}' requires that the Name property is set and evaluated to a string.");
							}

							// Create any child items
							switch (typeof(TItem).Name)
							{
								case nameof(Dashboard):
									// Ensure that the name and id are set
									if (
											item.Type != ItemSpecType.CloneSingleFromId
											|| !int.TryParse(item.Config, out var cloneFromIdValue))
									{
										throw new ConfigurationException($"Creating items of type '{typeof(TItem).Name}' requires that the CloneFromId property is set and evaluates as an integer.");
									}

									var originalDashboard = await logicMonitorClient
										.GetAsync<Dashboard>(cloneFromIdValue, cancellationToken)
										.ConfigureAwait(false);

									var newDashboard = await logicMonitorClient
										.CloneAsync(cloneFromIdValue,
										new DashboardCloneRequest
										{
											Name = item.Fields.TryEvaluate<string>("Name", variables, out var name)
												? name
												: originalDashboard.Name,
											Description = item.Fields.TryEvaluate<string>("Description", variables, out var description)
												? description
												: originalDashboard.Description,
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
							currentGroup,
							groupVariableName,
							logger,
							cancellationToken)
							.ConfigureAwait(false);
					}

					variables[groupVariableName] = currentGroup.Id;

					// Import any items
					await ImportItemsAsync<TGroup, TItem>(
						logicMonitorClient,
						currentGroup,
						structure.Items,
						variables,
						cancellationToken)
						.ConfigureAwait(false);

					break;
				default:
					// Unexpected mode
					var message = $"Unexpected mode: {mode}";
					logger.LogError(message);
					throw new ConfigurationException(message);
			}
		}

		private static async Task ImportItemsAsync<TGroup, TItem>(
			LogicMonitorClient logicMonitorClient,
			TGroup currentGroup,
			List<ItemSpec>? itemSpecs,
			Dictionary<string, object?> variables,
			CancellationToken cancellationToken)
			where TGroup : IdentifiedItem, IHasEndpoint, new()
			where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			if (itemSpecs is null)
			{
				return;
			}
			foreach (var itemSpec in itemSpecs)
			{
				switch (itemSpec.Type)
				{
					case ItemSpecType.XlsxMulti:
						{
							var fileAndSheetInfo = new FileAndSheetInfo(
								itemSpec.Config?.Evaluate<string>(variables)
								?? throw new ConfigurationException($"{ItemSpecType.XlsxMulti} import items should have the config set."));
							using var magicSpreadsheet = new MagicSpreadsheet(fileAndSheetInfo.FileInfo);
							magicSpreadsheet.Load();

							switch (currentGroup)
							{
								case NetscanGroup netscanGroup:
									// Create netscan groups from spreadsheet
									var objectList = magicSpreadsheet
										.GetExtendedList<object>(fileAndSheetInfo.SheetName);
									var netscanCreationDtos = await objectList.Where(obj => !obj.Properties.Any(p => p.Key == "Include" && p.Value is bool include && !include))
										.EvaluateAsync<NetscanCreationDto>(
											itemSpec,
											logicMonitorClient,
											variables,
											cancellationToken)
										.ConfigureAwait(false);

									foreach (var netscanCreationDto in netscanCreationDtos)
									{
										// Delete any existing
										var existingNetscans = await logicMonitorClient.GetAllAsync(
											new Filter<Netscan>
											{
												FilterItems = new List<FilterItem<Netscan>>{
													new Eq<Netscan>(nameof(Netscan.GroupId), netscanCreationDto.GroupId),
													new Eq<Netscan>(nameof(Netscan.Name), netscanCreationDto.Name),
												}
											},
											cancellationToken)
											.ConfigureAwait(false);
										if (existingNetscans.Count == 1)
										{
											await logicMonitorClient.DeleteAsync(existingNetscans[0], cancellationToken: cancellationToken)
												.ConfigureAwait(false);
										}
										// Create the new one
										await logicMonitorClient.CreateAsync<Netscan>(netscanCreationDto, cancellationToken)
											.ConfigureAwait(false);
									}
									break;
							}
						}
						break;
					default:
						throw new NotSupportedException($"ItemSpec type {itemSpec.Type} not supported.");
				}
			}
		}

		private static async Task<TGroup> CreateGroupAsync<TGroup, TItem>(
			LogicMonitorClient portalClient,
			TGroup? parentGroup,
			Structure<TGroup, TItem> structure,
			Dictionary<string, object?> variables)
			where TGroup : IdentifiedItem, IHasEndpoint, new()
			where TItem : IdentifiedItem, IHasEndpoint, new()
		{
			var name = structure.Name.Evaluate<string>(variables);
			var description = structure.Description.Evaluate<string>(variables);
			var properties = structure.Properties.Evaluate(variables);
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
					AppliesTo = structure.AppliesTo?.Evaluate<string>(variables),
					CustomProperties = properties,
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
			Task = RunAsync(TokenSource.Token);
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