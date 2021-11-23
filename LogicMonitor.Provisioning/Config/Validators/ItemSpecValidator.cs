namespace LogicMonitor.Provisioning.Config.Validators;

internal class ItemSpecValidator : AbstractValidator<ItemSpec>
{
	public ItemSpecValidator()
	{
		RuleFor(i => i.Type).IsInEnum();
		RuleFor(i => i.Config)
			.NotEmpty()
			.When(i => new[]
				{
						ItemSpecType.XlsxMulti,
						ItemSpecType.CloneSingleFromId
				}
				.Contains(i.Type)
			);
	}
}
