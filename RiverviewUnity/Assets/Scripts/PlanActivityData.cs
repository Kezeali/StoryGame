using UnityEngine;

namespace NotABear
{

	[CreateAssetMenu(fileName="PlanActivity.asset", menuName="Riverview/Plan Activity")]
	public class PlanActivityData : ScriptableObject, IDataItem
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

}