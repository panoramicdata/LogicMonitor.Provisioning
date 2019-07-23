using System.Collections.Generic;

namespace LogicMonitor.Provisioning.Config
{
	public class Structure<TGroup, TItem>
	{
		public bool Enabled { get; set; }
		public string Root { get; set; }
		public string Name { get; set; }
		public List<Structure<TGroup, TItem>> Groups { get; set; }
		public List<TItem> Items { get; set; }
	}
}