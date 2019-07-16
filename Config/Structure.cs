using System.Collections.Generic;

namespace LogicMonitor.Provisioning.Config
{
	public class Structure<TGroup, TItem>
	{
		public string Root { get; set; }
		public List<Structure<TGroup, TItem>> Groups { get; set; }
		public List<TItem> Items { get; set; }
		public bool Enabled { get; set; }
	}
}