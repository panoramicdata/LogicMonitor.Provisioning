namespace LogicMonitor.Provisioning.Config.Validators;

internal class RepetitionValidator : AbstractValidator<Repetition>
{
	public RepetitionValidator()
	{
		RuleFor(r => r.Type).IsInEnum();
		RuleFor(r => r.Config).Empty().When(r => r.Type == RepetitionType.None).WithMessage("Repetition.Config should be empty when repetition type is 'None'");
		RuleFor(r => r.Config).NotEmpty().When(r => r.Type == RepetitionType.Xlsx).WithMessage("Repetition.Config should not be empty when repetition type is 'Xlsx'");
	}
}
