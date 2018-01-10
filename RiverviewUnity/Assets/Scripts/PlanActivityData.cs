using UnityEngine;

namespace NotABear
{

	[CreateAssetMenu(fileName="PlanActivity.asset", menuName="Riverview/Plan Activity")]
	public class PlanActivityData : ScriptableObject
	{
		public StatBonusData[] statBonuses;
	}

	[System.Serializable]
	public class StatBonusData
	{
		public string name;
		public CharacterStat stat;
		public int bonusPerTimeUnit;
	}

	public class CharacterStat //: ScriptableObject
	{
	}

}