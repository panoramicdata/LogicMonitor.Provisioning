using System.Collections.Generic;

namespace LogicMonitor.Provisioning.Config
{
	public class ItemSpec
	{
		/// <summary>
		/// The type
		/// </summary>
		public ItemSpecType Type { get; set; }

		/// <summary>
		/// Varies by type.  Evaluated.
		/// </summary>
		public string? Config { get; set; }

		/// <summary>
		/// Used for fields.  Each is evaluated.
		/// </summary>
		public Dictionary<string, string> Fields { get; set; } = new();

		/// <summary>
		/// Used for properties.  Each is evaluated.
		/// </summary>
		public Dictionary<string, string> Properties { get; set; } = new();
	}
}