using UnityEngine;

namespace Cloverview
{

[CreateAssetMenu(fileName="DataIndex.asset", menuName="Cloverview/DataIndex")]
public class DataIndex : ScriptableObject
{
	public RoleData playerRole;
	public RoleData[] roles;
	public OutfitItemData[] outfitItems;
	public CharacterStatDefinition[] characterStats;
	public QualityData[] qualities;

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
