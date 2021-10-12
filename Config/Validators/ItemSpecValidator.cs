using FluentValidation;

namespace LogicMonitor.Provisioning.Config.Validators
{
	internal class ItemSpecValidator : AbstractValidator<ItemSpec>
	{
		public ItemSpecValidator()
		{
			RuleFor(i => i.Name).NotEmpty().Unless(i => i.CloneFromId is not null);
			RuleFor(i => i.Description).NotEmpty();
		}
	}
}