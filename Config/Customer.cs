using System.Collections.Generic;

namespace LogicMonitor.Provisioning.Config
{
	public class Customer
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public List<Property> Properties { get; set; }
	}
}
