namespace LogicMonitor.Provisioning.Config
{
	public class StructureSpecification<T> : ProvisioningGroup
	{
		public string Root { get; set; }
		public Structure<T> Structure { get; set; }
	}
}