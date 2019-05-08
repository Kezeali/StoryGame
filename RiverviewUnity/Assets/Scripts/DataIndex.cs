using UnityEngine;

namespace Cloverview
{

// Asset type which acts as an index of other asset objects for the game.
[CreateAssetMenu(fileName="DataIndex.asset", menuName="Cloverview/DataIndex")]
public class DataIndex : ScriptableObject
{
	[Tooltip("The PC. Should also be included in the 'roles' list.")]
	public RoleData playerRole;

	public RoleData[] roles;
	public OutfitItemData[] outfitItems;
	public QualityData[] qualities;
	public CharacterStatDefinition[] characterStats;
	public StatBonusData[] statBonuses;

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
