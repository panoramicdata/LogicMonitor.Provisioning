namespace LogicMonitor.Provisioning.Config.Validators;

internal class LogicMonitorCredentialsValidator : AbstractValidator<LogicMonitorCredentials>
{
	public LogicMonitorCredentialsValidator()
	{
		RuleFor(c => c.Account).NotEmpty();
		RuleFor(c => c.AccessId).NotEmpty();
		RuleFor(c => c.AccessKey).NotEmpty();
	}
}
