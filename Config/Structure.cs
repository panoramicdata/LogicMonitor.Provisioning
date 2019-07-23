using System.Collections.Generic;

namespace LogicMonitor.Provisioning.Config
{
	public class Structure<TGroup, TItem>
	{
		public bool Enabled { get; set; } = true;
		public string Root { get; set; }
		public string Name { get; set; }
		public List<Structure<TGroup, TItem>> Groups { get; set; }
		public List<ItemSpec> Items { get; set; }
		public bool ApplyProperties { get; set; }
		public string AppliesTo { get; internal set; }
	}
}