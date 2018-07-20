using UnityEngine;

namespace Cloverview
{

	[CreateAssetMenu(fileName="PlanActivity.asset", menuName="Cloverview/Plan Activity Definition")]
	public class PlanActivityData : ScriptableObject, IDataItem
	{
		public SceneData scene;
		public SceneRole[] leadRoles;
		public CastingCharacterDescription[] extrasDescriptions;
		public StatBonusData[] statBonuses;
		public StatAffectorData[] statAffectors;
	}

	[System.Serializable]
	public struct SceneRole
	{
		public StageMarkData mark;
		public RoleData role;
		public CharacterProp[] props;
	}

}