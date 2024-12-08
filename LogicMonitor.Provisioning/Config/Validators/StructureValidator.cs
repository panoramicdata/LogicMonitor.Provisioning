namespace LogicMonitor.Provisioning.Config.Validators;

internal class StructureValidator<T1, T2> : AbstractValidator<Structure<T1, T2>?>
{
	public StructureValidator(bool isRoot = true)
		=> RuleFor(s => s)
			.NotNull()
			.WithMessage($"Structure<{typeof(T1).Name}, {typeof(T2).Name}>. Structure should not be null")
			.DependentRules(() =>
			{
				RuleFor(s => s!.Name)
					.NotEmpty()
					.WithMessage($"Structure<{typeof(T1).Name}, {typeof(T2).Name}>. Name should not be empty");

				RuleFor(s => s!.Description)
					.NotEmpty();

				RuleForEach(s => s!.Properties)
					.Must(a => !string.IsNullOrWhiteSpace(a.Value));

				RuleFor(s => s!.Groups)
					.Must(BeValidChildGroup);

				RuleForEach(s => s!.Items)
					.SetValidator(new ItemSpecValidator());

				RuleFor(s => s!.Parent)
					.Null()
					.When(_ => !isRoot)
					.WithMessage($"Structure<{typeof(T1).Name}, {typeof(T2).Name}>. Parent should be null when node is not root.");
			});

	private bool BeValidChildGroup(List<Structure<T1, T2>>? list)
	{
		if (list == null || list.Count == 0)
		{
			return true;
		}

		foreach (var child in list)
		{
			var validator = new StructureValidator<T1, T2>(false);
			var validatorResults = validator.Validate(child);
			if (!validatorResults.IsValid)
			{
				return false;
			}
		}

		return true;
	}
}
