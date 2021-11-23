using FluentValidation;
using LogicMonitor.Api.Collectors;
using LogicMonitor.Api.Dashboards;
using LogicMonitor.Api.Devices;
using LogicMonitor.Api.Netscans;
using LogicMonitor.Api.Reports;
using LogicMonitor.Api.Topologies;
using LogicMonitor.Api.Users;
using LogicMonitor.Api.Websites;

namespace LogicMonitor.Provisioning.Config.Validators;

internal class ConfigurationValidator : AbstractValidator<Configuration>
{
	public ConfigurationValidator()
	{
		RuleFor(config => config.LogicMonitorCredentials).SetValidator(new LogicMonitorCredentialsValidator());
		RuleFor(config => config.Repetition).SetValidator(new RepetitionValidator());
		RuleForEach(config => config.RoleConfigurations).SetValidator(new RoleConfigurationValidator());

		RuleFor(config => config.Collectors).SetValidator(new StructureValidator<CollectorGroup, Collector>());
		RuleFor(config => config.Dashboards).SetValidator(new StructureValidator<DashboardGroup, Dashboard>());
		RuleFor(config => config.Resources).SetValidator(new StructureValidator<DeviceGroup, Device>());
		RuleFor(config => config.Mappings).SetValidator(new StructureValidator<TopologyGroup, Topology>());
		RuleFor(config => config.Netscans).SetValidator(new StructureValidator<NetscanGroup, Netscan>());
		RuleFor(config => config.Reports).SetValidator(new StructureValidator<ReportGroup, Report>());
		RuleFor(config => config.Roles).SetValidator(new StructureValidator<RoleGroup, Role>());
		RuleFor(config => config.Users).SetValidator(new StructureValidator<UserGroup, User>());
		RuleFor(config => config.Websites).SetValidator(new StructureValidator<WebsiteGroup, Website>());
	}
}
