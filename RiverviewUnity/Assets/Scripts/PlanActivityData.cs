using UnityEngine;

namespace Cloverview
{

	[CreateAssetMenu(fileName="PlanActivity.asset", menuName="Cloverview/Plan Activity Definition")]
	public class PlanActivityData : ScriptableObject, IDataItem
	{
		public SceneData scene;
		public StatBonusData[] statBonuses;
		public SceneRole[] leadRoles;
		public CastingCharacterDescription[] extrasDescriptions;
	}

	[System.Serializable]
	public struct SceneRole
	{
		public StageMarkData mark;
		public RoleData role;
		public CharacterProp[] props;
	}

}