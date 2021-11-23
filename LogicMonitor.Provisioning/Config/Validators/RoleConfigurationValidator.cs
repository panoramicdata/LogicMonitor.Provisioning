using FluentValidation;

namespace LogicMonitor.Provisioning.Config.Validators;

internal class RoleConfigurationValidator : AbstractValidator<RoleConfiguration>
{
	public RoleConfigurationValidator()
	{
		RuleFor(rc => rc.Name).NotEmpty();
		RuleFor(rc => rc.Description).NotEmpty();
		RuleFor(rc => rc.AccessLevel).IsInEnum();
	}
}
