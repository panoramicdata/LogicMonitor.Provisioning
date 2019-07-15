using System.Collections.Generic;

namespace LogicMonitor.Provisioning.Config
{
	public class Structure<T>
	{
		public List<Structure<T>> Groups { get; set; }

		public List<T> Items { get; set; }
	}
}