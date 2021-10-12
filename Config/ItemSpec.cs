namespace LogicMonitor.Provisioning.Config
{
	public class ItemSpec
	{
		public string Name { get; set; } = string.Empty;

		public int? CloneFromId { get; set; }

		public string Description { get; set; } = string.Empty;
	}
}