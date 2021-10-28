using LogicMonitor.Api.Users;

namespace LogicMonitor.Provisioning
{
	internal class RoleSpec
	{
		public RoleSpec(
			PrivilegeObjectType privilegeObjectType,
			string? text,
			RolePrivilegeOperation rolePrivilegeOperation = RolePrivilegeOperation.Write)
		{
			ObjectType = privilegeObjectType;
			ObjectId = text;
			RolePrivilegeOperation = rolePrivilegeOperation;
		}

		public PrivilegeObjectType ObjectType { get; }
		public object? ObjectId { get; }
		public RolePrivilegeOperation RolePrivilegeOperation { get; }
	}
}