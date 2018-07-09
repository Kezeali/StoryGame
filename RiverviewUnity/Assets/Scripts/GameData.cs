using UnityEngine;

namespace Cloverview
{

[CreateAssetMenu(fileName="Data.asset", menuName="Cloverview/Data Index")]
public class GameData : ScriptableObject
{
	public RoleData playerRole;
	public RoleData[] roles;

	public static RoleData FindRole(RoleData[] roles, string roleName)
	{
		RoleData result = null;
		for (int i = 0; i < roles.Length; ++i) {
			if (roles[i].name == roleName) {
				result = roles[i];
				break;
			}
		}
		return result;
	}
}

}
