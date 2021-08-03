using System.Collections.Generic;

namespace LogicMonitor.Provisioning.Config
{
	/// <summary>
	/// A creation structure
	/// </summary>
	/// <typeparam name="TGroup">The group type</typeparam>
	/// <typeparam name="TItem">The item type</typeparam>
	public class Structure<TGroup, TItem>
	{
		/// <summary>
		/// Whether this structure is enabled (default: true)
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// The name
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// The description
		/// </summary>
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// Any subgroups to create (default: empty list)
		/// </summary>
		public List<Structure<TGroup, TItem>> Groups { get; set; } = new();

		/// <summary>
		/// Any items to create (default: empty list)
		/// </summary>
		public List<ItemSpec> Items { get; set; } = new();

		/// <summary>
		/// Whether to apply properties (default: false)
		/// </summary>
		public bool ApplyProperties { get; set; }

		/// <summary>
		/// Where relevant: the appliesTo (default: null)
		/// </summary>
		public string? AppliesTo { get; set; }
	}
}