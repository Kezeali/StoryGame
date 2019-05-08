using UnityEngine;

namespace Cloverview
{

	// Data type for plan activities (see ActiveActivity)
	[CreateAssetMenu(fileName="PlanActivity.asset", menuName="Cloverview/Plan Activity Definition")]
	public class PlanActivityData : ScriptableObject, IDataItem
	{
		public SceneData scene;
		public SceneRole[] leadRoles;
		public CastingCharacterDescription[] extrasDescriptions;
		public StatBonusData[] statBonuses;
		public StatAffectorData[] statAffectors;
	}

	// A "role" for a character in the scene. Used to populate the scene with named characters.
	[System.Serializable]
	public struct SceneRole
	{
		public StageMarkData mark;
		public RoleData role;
		public CharacterProp[] props;
	}

}